using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class ResultCreateDto
{
    /// <example>1</example>
    [Required]
    public int EnrolmentId { get; set; }

    /// <summary>Finish time in the format hh:mm:ss.</summary>
    /// <example>01:45:30</example>
    public string? FinishTime { get; set; }

    /// <example>12</example>
    [Range(1, int.MaxValue)]
    public int? Position { get; set; }

    /// <summary>Finished, DNF (did not finish) or DNS (did not start).</summary>
    /// <example>Finished</example>
    [MaxLength(20)]
    public string Status { get; set; } = "Finished";
}

public class ResultUpdateDto
{
    /// <example>01:44:10</example>
    public string? FinishTime { get; set; }

    /// <example>10</example>
    [Range(1, int.MaxValue)]
    public int? Position { get; set; }

    /// <example>Finished</example>
    [MaxLength(20)]
    public string Status { get; set; } = "Finished";
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;

    public static ResultDto FromModel(Result r) => new ResultDto
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantName = r.Enrolment?.Participant?.FullName ?? "",
        EventName = r.Enrolment?.Category?.Event?.EventName ?? "",
        CategoryName = r.Enrolment?.Category?.CategoryName ?? "",
        FinishTime = r.FinishTime?.ToString(@"hh\:mm\:ss"),
        Position = r.Position,
        Status = r.Status
    };
}
