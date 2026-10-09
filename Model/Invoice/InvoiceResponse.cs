namespace AvecADeskApi.Model.Invoice;

public class InvoiceResponse
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public string? InstituteAddress { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? InvoiceMonthYear { get; set; }
    public string? PdfPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}
