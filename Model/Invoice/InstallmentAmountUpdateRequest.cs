namespace AvecADeskApi.Model.Invoice;

public class InstallmentAmountUpdateRequest
{
    public int InstallmentId { get; set; }
    public int? CommissionDetailId { get; set; }
    public decimal? FeesAmount { get; set; }
    public decimal? InvoiceAmount { get; set; }
}
public class BonusInstallmentRequest
{
    public int CommissionDetailId { get; set; }
    public int StudentPaymentInstallmentId { get; set; }
    public decimal BonusAmount { get; set; }
}