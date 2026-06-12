namespace AvecADeskApi.Model.PaymentSchedule;

public class PaymentScheduleResponse
{
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}
