using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>One line of a participant's history: the event, category, enrolment date and result (if any).</summary>
public class HistoryDto
{
    public int EnrolmentId { get; set; }

    // Event
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;

    // Category
    public string CategoryName { get; set; } = string.Empty;

    // Enrolment
    public DateTime EnrolmentDate { get; set; }
    public string EnrolmentStatus { get; set; } = string.Empty;

    // Result (null if no result has been captured yet)
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string? ResultStatus { get; set; }

    public static HistoryDto FromModel(Enrolment en) => new HistoryDto
    {
        EnrolmentId = en.EnrolmentId,
        EventId = en.Category?.EventId ?? 0,
        EventName = en.Category?.Event?.EventName ?? "",
        EventDate = en.Category?.Event?.EventDate ?? DateTime.MinValue,
        Location = en.Category?.Event?.Location ?? "",
        CategoryName = en.Category?.CategoryName ?? "",
        EnrolmentDate = en.EnrolmentDate,
        EnrolmentStatus = en.Status,
        FinishTime = en.Result?.FinishTime?.ToString(@"hh\:mm\:ss"),
        Position = en.Result?.Position,
        ResultStatus = en.Result?.Status
    };
}
