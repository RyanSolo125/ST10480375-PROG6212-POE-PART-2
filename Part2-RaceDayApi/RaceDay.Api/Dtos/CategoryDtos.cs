using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Used for both creating and updating a category.</summary>
public class CategoryCreateDto
{
    /// <example>Senior 10km</example>
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    /// <example>10</example>
    [Range(0, 999)]
    public decimal DistanceKm { get; set; }

    /// <example>200</example>
    [Range(0, 99999)]
    public decimal EntryFee { get; set; }

    /// <example>500</example>
    [Range(1, int.MaxValue)]
    public int? MaxParticipants { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }

    public static CategoryDto FromModel(Category c) => new CategoryDto
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKm,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants
    };
}
