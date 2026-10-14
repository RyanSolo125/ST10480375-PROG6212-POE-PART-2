using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Payment
{
    public int PaymentId { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    [MaxLength(30)]
    public string PaymentMethod { get; set; } = "Card";

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Paid";
}
