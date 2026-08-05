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
    Task<List<MonthlyPaidInstallmentRow>> GetPaidInstallmentsForMonthAsync(
        int year,
        int month,
        int? instituteId = null,
        string? campus = null);
    Task<List<MonthlyPaidInstallmentRow>> GetInstallmentsForMonthPreviewAsync(
        int year,
        int month,
        int? instituteId = null,
        string? campus = null);
    Task<InvoiceResponse?> GenerateMonthlyPaidStudentInvoiceAsync(
        int year,
        int month,
        int instituteId,
        string? campus = null);
    Task UpdateInvoicePdfPathAsync(int invoiceId, string pdfPath);
    Task<List<InvoiceLineItemResponse>> GetInvoiceLineItemsAsync(int invoiceId);
    Task<decimal> GetNextMonthInvoiceTotalAsync();
}
