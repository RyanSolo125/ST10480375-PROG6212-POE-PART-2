using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Used for both creating and updating an event.</summary>
public class EventCreateDto
{
    /// <example>Durban City Night Run</example>
    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    /// <example>A 10km night run along Durban's beachfront.</example>
    public string? Description { get; set; }

    /// <example>2026-12-05</example>
    [Required]
    public DateTime EventDate { get; set; }

    /// <example>Durban</example>
    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    /// <example>10</example>
    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    /// <example>Run</example>
    [Required]
    public string EventType { get; set; } = string.Empty;
}

public class EventDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public List<CategoryDto> Categories { get; set; } = new();

    public static EventDto FromModel(Event e) => new EventDto
    {
        EventId = e.EventId,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser?.FullName ?? "",
        Categories = e.Categories.Select(CategoryDto.FromModel).ToList()
    };
}
