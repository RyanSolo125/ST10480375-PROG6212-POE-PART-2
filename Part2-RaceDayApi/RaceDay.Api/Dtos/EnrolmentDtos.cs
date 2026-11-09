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

public class EnrolmentStatusDto
{
    /// <summary>Pending, Confirmed or Cancelled.</summary>
    /// <example>Confirmed</example>
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;

    // Result for this enrolment (null if not captured yet)
    public int? ResultId { get; set; }
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string? ResultStatus { get; set; }

    public static EnrolmentDto FromModel(Enrolment en) => new EnrolmentDto
    {
        EnrolmentId = en.EnrolmentId,
        ParticipantId = en.ParticipantId,
        ParticipantName = en.Participant?.FullName ?? "",
        ParticipantEmail = en.Participant?.Email ?? "",
        EventId = en.Category?.EventId ?? 0,
        EventName = en.Category?.Event?.EventName ?? "",
        EventDate = en.Category?.Event?.EventDate ?? DateTime.MinValue,
        Location = en.Category?.Event?.Location ?? "",
        CategoryId = en.CategoryId,
        CategoryName = en.Category?.CategoryName ?? "",
        EnrolmentDate = en.EnrolmentDate,
        Status = en.Status,
        ResultId = en.Result?.ResultId,
        FinishTime = en.Result?.FinishTime?.ToString(@"hh\:mm\:ss"),
        Position = en.Result?.Position,
        ResultStatus = en.Result?.Status
    };
}
