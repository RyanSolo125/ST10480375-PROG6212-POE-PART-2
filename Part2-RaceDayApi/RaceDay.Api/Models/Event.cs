using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }

    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column(TypeName = "date")]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Column(TypeName = "decimal(6,2)")]
    public decimal DistanceKm { get; set; }

    [Required, MaxLength(50)]
    public string EventType { get; set; } = "Run"; // Run, Walk or Cycle

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Category> Categories { get; set; } = new();
}
