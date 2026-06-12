namespace AvecADeskApi.Model.PaymentSchedule;

public class PaymentScheduleUpdateRequest
{
    public int StudentId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public string? Notes { get; set; }
}
