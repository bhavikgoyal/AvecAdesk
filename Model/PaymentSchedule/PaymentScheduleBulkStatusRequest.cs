namespace AvecADeskApi.Model.PaymentSchedule;

public class PaymentScheduleBulkStatusRequest
{
    public List<PaymentScheduleBulkStatusItem> Items { get; set; } = new();
}
