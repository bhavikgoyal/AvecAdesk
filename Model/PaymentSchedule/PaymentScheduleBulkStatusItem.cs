namespace AvecADeskApi.Model.PaymentSchedule;

public class PaymentScheduleBulkStatusItem
{
    public int ScheduleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? AmountPaid { get; set; }
}
