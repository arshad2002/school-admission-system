using System.ComponentModel.DataAnnotations;

namespace AdmissionSystem.Api.Models;

public class ClassRoom
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int TotalSeats { get; set; }

    public ICollection<SeatHold> Holds { get; set; } = new List<SeatHold>();
}