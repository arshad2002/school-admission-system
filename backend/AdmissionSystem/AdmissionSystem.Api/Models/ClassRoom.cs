using System.ComponentModel.DataAnnotations;

namespace AdmissionSystem.Api.Models;

public class ClassRoom
{
    [Key]
    public string Id { get; set; } = $"c_{Guid.NewGuid():N}";

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int TotalSeats { get; set; }

    /// <summary>
    /// Denormalized counter: incremented exactly once when a hold is confirmed.
    /// Protected by row-level locking during seat mutations.
    /// </summary>
    public int ConfirmedSeats { get; set; }

    /// <summary>
    /// EF Core optimistic concurrency token — Postgres xmin system column.
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }

    public ICollection<SeatHold> Holds { get; set; } = new List<SeatHold>();
}