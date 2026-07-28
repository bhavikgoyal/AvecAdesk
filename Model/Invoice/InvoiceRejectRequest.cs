namespace AvecADeskApi.Model.Invoice;

public class InvoiceRejectRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}

public class InvoiceLineItemResponse
{
    public int LineItemId { get; set; }
    public int InvoiceId { get; set; }
    public int StudentId { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}
