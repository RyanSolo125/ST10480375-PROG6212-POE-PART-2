using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty; // e.g. "Under 20", "Senior", "10km"

    [Column(TypeName = "decimal(5,2)")]
    public decimal DistanceKm { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal EntryFee { get; set; } = 0;

    public int? MaxParticipants { get; set; }

    public List<Enrolment> Enrolments { get; set; } = new();
}
