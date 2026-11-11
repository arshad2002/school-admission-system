using AdmissionSystem.Api.Data;
using AdmissionSystem.Api.DTOs;
using AdmissionSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdmissionSystem.Api.Services;

public sealed class AdmissionService
{
    private readonly ApplicationDbContext _db;
    private readonly int _holdDurationSeconds;
    private readonly ILogger<AdmissionService> _logger;

    public AdmissionService(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger<AdmissionService> logger)
    {
        _db = db;
        _logger = logger;

        var envValue = Environment.GetEnvironmentVariable("HOLD_DURATION_SECONDS");
        _holdDurationSeconds = int.TryParse(envValue, out var parsed) ? parsed : 600;
    }

    // ── Helpers ─────────────────────────────────────────────

    /// <summary>
    /// Resolves the effective status of a hold: if the stored status is HELD
    /// but ExpiresAt is in the past, it is treated as EXPIRED.
    /// </summary>
    private static string EffectiveStatus(SeatHold hold)
    {
        if (hold.Status == "HELD" && hold.ExpiresAt <= DateTime.UtcNow)
            return "EXPIRED";
        return hold.Status;
    }

    /// <summary>
    /// Counts seats that are currently actively held (HELD status and not yet expired).
    /// </summary>
    private async Task<int> CountActiveHeldSeatsAsync(string classId)
    {
        return await _db.Holds
            .Where(h => h.ClassId == classId
                        && h.Status == "HELD"
                        && h.ExpiresAt > DateTime.UtcNow)
            .CountAsync();
    }

    // ── Class Operations ────────────────────────────────────

    public async Task<CreateClassResponse> CreateClassAsync(CreateClassRequest request)
    {
        var classRoom = new ClassRoom
        {
            Name = request.Name,
            TotalSeats = request.TotalSeats
        };

        _db.Classes.Add(classRoom);
        await _db.SaveChangesAsync();

        return new CreateClassResponse(classRoom.Id, classRoom.Name, classRoom.TotalSeats);
    }

    public async Task<List<ClassDetailResponse>> ListClassesAsync()
    {
        var now = DateTime.UtcNow;

        var classes = await _db.Classes
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                c.ConfirmedSeats,
                ActiveHeldSeats = c.Holds
                    .Count(h => h.Status == "HELD" && h.ExpiresAt > now)
            })
            .ToListAsync();

        return classes.Select(c => new ClassDetailResponse(
            c.Id,
            c.Name,
            c.TotalSeats,
            c.ActiveHeldSeats,
            c.ConfirmedSeats,
            Math.Max(0, c.TotalSeats - c.ConfirmedSeats - c.ActiveHeldSeats)
        )).ToList();
    }

    public async Task<ClassDetailResponse?> GetClassAsync(string classId)
    {
        var now = DateTime.UtcNow;

        var result = await _db.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                c.ConfirmedSeats,
                ActiveHeldSeats = c.Holds
                    .Count(h => h.Status == "HELD" && h.ExpiresAt > now)
            })
            .FirstOrDefaultAsync();

        if (result is null) return null;

        return new ClassDetailResponse(
            result.Id,
            result.Name,
            result.TotalSeats,
            result.ActiveHeldSeats,
            result.ConfirmedSeats,
            Math.Max(0, result.TotalSeats - result.ConfirmedSeats - result.ActiveHeldSeats));
    }

    // ── Hold Operations (Concurrency-Safe) ──────────────────

    /// <summary>
    /// Creates a seat hold using PostgreSQL row-level locking (SELECT … FOR UPDATE)
    /// to guarantee that (ActiveHeld + Confirmed) never exceeds TotalSeats.
    /// </summary>
    public async Task<(HoldResponse? Hold, string? Error)> CreateHoldAsync(
        string classId, string studentId)
    {
        // Use an explicit transaction with Serializable-equivalent row-level lock
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // ── 1. Lock the class row with FOR UPDATE ──
            var classRoom = await _db.Classes
                .FromSqlInterpolated(
                    $"SELECT * FROM \"Classes\" WHERE \"Id\" = {classId} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (classRoom is null)
                return (null, "Class not found");

            // ── 2. Check for duplicate active hold / confirmed admission ──
            var existingHold = await _db.Holds
                .Where(h => h.ClassId == classId
                            && h.StudentId == studentId
                            && (h.Status == "CONFIRMED"
                                || (h.Status == "HELD" && h.ExpiresAt > DateTime.UtcNow)))
                .FirstOrDefaultAsync();

            if (existingHold is not null)
                return (null, "Student already has an active hold or confirmed admission for this class");

            // ── 3. Calculate available seats ──
            var activeHeldSeats = await CountActiveHeldSeatsAsync(classId);
            var availableSeats = classRoom.TotalSeats - classRoom.ConfirmedSeats - activeHeldSeats;

            if (availableSeats <= 0)
                return (null, "No available seats");

            // ── 4. Create the hold ──
            var hold = new SeatHold
            {
                ClassId = classId,
                StudentId = studentId,
                Status = "HELD",
                ExpiresAt = DateTime.UtcNow.AddSeconds(_holdDurationSeconds)
            };

            _db.Holds.Add(hold);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (new HoldResponse(
                hold.HoldId,
                hold.ClassId,
                hold.StudentId,
                hold.Status,
                hold.ExpiresAt), null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create hold for class {ClassId}, student {StudentId}",
                classId, studentId);
            throw;
        }
    }

    public async Task<HoldStatusResponse?> GetHoldAsync(string holdId)
    {
        var hold = await _db.Holds
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HoldId == holdId);

        if (hold is null) return null;

        return new HoldStatusResponse(
            hold.HoldId,
            hold.ClassId,
            EffectiveStatus(hold),
            hold.ExpiresAt);
    }

    /// <summary>
    /// Confirms a hold. Uses row-level locking to atomically increment ConfirmedSeats.
    /// Idempotent: re-confirming an already-confirmed hold returns success without
    /// double-incrementing the counter.
    /// </summary>
    public async Task<(ConfirmHoldResponse? Response, int StatusCode, string? Error)> ConfirmHoldAsync(
        string holdId, string studentId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // Lock the hold row
            var hold = await _db.Holds
                .FromSqlInterpolated(
                    $"SELECT * FROM \"Holds\" WHERE \"HoldId\" = {holdId} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (hold is null)
                return (null, 404, "Hold not found");

            if (hold.StudentId != studentId)
                return (null, 403, "Hold does not belong to this student");

            // Idempotent: already confirmed
            if (hold.Status == "CONFIRMED")
            {
                await transaction.CommitAsync();
                return (new ConfirmHoldResponse(hold.HoldId, "CONFIRMED"), 200, null);
            }

            // Must be HELD and not expired
            if (hold.Status != "HELD")
                return (null, 409, $"Hold is in '{hold.Status}' status and cannot be confirmed");

            if (hold.ExpiresAt <= DateTime.UtcNow)
                return (null, 409, "Hold has expired");

            // Lock the class row and increment ConfirmedSeats
            var classRoom = await _db.Classes
                .FromSqlInterpolated(
                    $"SELECT * FROM \"Classes\" WHERE \"Id\" = {hold.ClassId} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (classRoom is null)
                return (null, 404, "Class not found");

            hold.Status = "CONFIRMED";
            classRoom.ConfirmedSeats += 1;

            // EF Core is tracking both entities from the raw SQL; save changes
            _db.Entry(hold).State = EntityState.Modified;
            _db.Entry(classRoom).State = EntityState.Modified;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (new ConfirmHoldResponse(hold.HoldId, "CONFIRMED"), 200, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to confirm hold {HoldId}", holdId);
            throw;
        }
    }

    /// <summary>
    /// Cancels a held seat. If the hold was confirmed, decrements ConfirmedSeats.
    /// </summary>
    public async Task<(int StatusCode, string? Error)> CancelHoldAsync(
        string holdId, string studentId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var hold = await _db.Holds
                .FromSqlInterpolated(
                    $"SELECT * FROM \"Holds\" WHERE \"HoldId\" = {holdId} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (hold is null)
                return (404, "Hold not found");

            if (hold.StudentId != studentId)
                return (403, "Hold does not belong to this student");

            var effectiveStatus = EffectiveStatus(hold);

            if (effectiveStatus is "CANCELLED")
                return (204, null); // Already cancelled — idempotent

            // If confirmed, decrement the class counter
            if (hold.Status == "CONFIRMED")
            {
                var classRoom = await _db.Classes
                    .FromSqlInterpolated(
                        $"SELECT * FROM \"Classes\" WHERE \"Id\" = {hold.ClassId} FOR UPDATE")
                    .FirstOrDefaultAsync();

                if (classRoom is not null)
                {
                    classRoom.ConfirmedSeats = Math.Max(0, classRoom.ConfirmedSeats - 1);
                    _db.Entry(classRoom).State = EntityState.Modified;
                }
            }

            hold.Status = "CANCELLED";
            _db.Entry(hold).State = EntityState.Modified;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (204, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to cancel hold {HoldId}", holdId);
            throw;
        }
    }

    // ── Student Holds ───────────────────────────────────────

    public async Task<List<MyHoldResponse>> GetStudentHoldsAsync(string studentId)
    {
        var holds = await _db.Holds
            .AsNoTracking()
            .Include(h => h.ClassRoom)
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.ExpiresAt)
            .ToListAsync();

        return holds.Select(h => new MyHoldResponse(
            h.HoldId,
            h.ClassId,
            h.ClassRoom.Name,
            EffectiveStatus(h),
            h.ExpiresAt
        )).ToList();
    }
}
