namespace AvecADeskApi.Model.Commission;

public class CommissionForecastResponse
{
    public int VendorId { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalApproved { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal AnticipatedAmount { get; set; }
    public int RecordCount { get; set; }
}
