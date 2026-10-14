using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }

    public TimeSpan? FinishTime { get; set; } // maps to SQL TIME

    public int? Position { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Finished";
}
