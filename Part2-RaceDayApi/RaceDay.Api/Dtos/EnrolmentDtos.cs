using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class EnrolmentCreateDto
{
    /// <summary>The category (within an event) the participant wants to enter.</summary>
    /// <example>5</example>
    [Required]
    public int CategoryId { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public static EnrolmentDto FromModel(Enrolment en) => new EnrolmentDto
    {
        EnrolmentId = en.EnrolmentId,
        ParticipantId = en.ParticipantId,
        ParticipantName = en.Participant?.FullName ?? "",
        ParticipantEmail = en.Participant?.Email ?? "",
        EventId = en.Category?.EventId ?? 0,
        EventName = en.Category?.Event?.EventName ?? "",
        CategoryId = en.CategoryId,
        CategoryName = en.Category?.CategoryName ?? "",
        EnrolmentDate = en.EnrolmentDate,
        Status = en.Status
    };
}
