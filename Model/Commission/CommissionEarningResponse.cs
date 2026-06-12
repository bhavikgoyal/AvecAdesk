namespace AvecADeskApi.Model.Commission;

public class CommissionEarningResponse
{
    public int EarningId { get; set; }
    public int VendorId { get; set; }
    public int CommissionId { get; set; }
    public int StudentPaymentId { get; set; }
    public decimal EarnedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
