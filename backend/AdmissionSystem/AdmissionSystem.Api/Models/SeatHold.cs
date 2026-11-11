using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdmissionSystem.Api.Models;

public class SeatHold
{
    [Key]
    public string HoldId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string ClassId { get; set; } = string.Empty;

    [Required]
    [MaxLength(17)]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "HELD"; 

    [Required]
    public DateTime ExpiresAt { get; set; }

    [ForeignKey(nameof(ClassId))]
    public ClassRoom ClassRoom { get; set; } = null!;
}