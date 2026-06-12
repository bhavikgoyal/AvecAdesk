namespace AvecADeskApi.Model.PaymentSchedule;

public class PaymentScheduleStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public decimal? AmountPaid { get; set; }
}
