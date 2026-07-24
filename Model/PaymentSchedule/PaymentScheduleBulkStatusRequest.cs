namespace AvecADeskApi.Model.PaymentSchedule;

public class PaymentScheduleBulkStatusRequest
{
    public List<PaymentScheduleBulkStatusItem> Items { get; set; } = new();
}
public class UpdateStudentPaymentScheduleRequest
{
    public int StudentId { get; set; }
    public int NoOfInstallments { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public DateTime FirstDueDate { get; set; }
}