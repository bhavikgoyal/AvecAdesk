using AvecADeskApi.Model.Invoice;

namespace AvecADeskApi.Interfaces;

public interface IInvoiceRepository
{
    Task<List<InvoiceResponse>> GetInvoicesAsync();
    Task<InvoiceResponse?> GetInvoiceByIdAsync(int invoiceId);
    Task<int> GenerateInvoiceAsync(int uploadId);
    Task<bool> SubmitInvoiceAsync(int invoiceId);
    Task<InvoiceResponse?> ApproveInvoiceAsync(int invoiceId, int? approvedByUserId);
    Task<bool> RejectInvoiceAsync(int invoiceId, string rejectionReason);
    Task<string?> GetInvoicePdfPathAsync(int invoiceId);
}
