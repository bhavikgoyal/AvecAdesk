namespace AvecADeskApi.Model.Invoice
{
    public class InvoiceLineItemAmountUpdateRequest
    {
        public int LineItemId { get; set; }
        public decimal Amount { get; set; }
    }
}
