namespace AdmissionSystem.Api.DTOs;

// ── Requests ────────────────────────────────────────────────
public sealed record CreateClassRequest(string Name, int TotalSeats);

// ── Responses ───────────────────────────────────────────────
public sealed record CreateClassResponse(string Id, string Name, int TotalSeats);

public sealed record ClassDetailResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);
