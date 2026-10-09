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
    public string? StudentName { get; set; }
    public string? EnrollmentNo { get; set; }
    public string? Description { get; set; }
    public string? CricosCode { get; set; }
    public decimal Amount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal FeesAmount { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal GSTAmount { get; set; }
    public string? Address { get; set; }
    public string? ZipCode { get; set; }
}
