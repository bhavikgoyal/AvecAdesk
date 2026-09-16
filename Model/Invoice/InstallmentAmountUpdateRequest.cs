namespace AvecADeskApi.Model.Invoice;

public class InstallmentAmountUpdateRequest
{
    public int InstallmentId { get; set; }
    public decimal? FeesAmount { get; set; }
    public decimal? InvoiceAmount { get; set; }
}
