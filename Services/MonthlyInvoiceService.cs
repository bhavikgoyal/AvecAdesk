
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Invoice;
using Azure.Core;

namespace AvecADeskApi.Services;

public class MonthlyInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IEmailSender _emailSender;
    private readonly InvoiceDocumentService _documentService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly LogHelper _logHelper;

    public MonthlyInvoiceService(
        IInvoiceRepository invoiceRepository,
        IEmailSender emailSender,
        InvoiceDocumentService documentService,
        IConfiguration configuration,
        IWebHostEnvironment env,
        LogHelper logHelper)
    {
        _invoiceRepository = invoiceRepository;
        _emailSender = emailSender;
        _documentService = documentService;
        _configuration = configuration;
        _env = env;
        _logHelper = logHelper;
    }

    public async Task<List<MonthlyPaidInstallmentRow>> GetPaidStudentsAsync( int? year = null, int? month = null, int? instituteId = null, string? campus = null)
    {
        var (targetYear, targetMonth) = ResolvePeriod(year, month);
        // Preview list: all installments due in the month (Paid + Pending, etc.)
        return await _invoiceRepository.GetInstallmentsForMonthPreviewAsync( targetYear, targetMonth, instituteId, campus);
    }

    
    public async Task<MonthlyInvoiceGenerateResult> GenerateAndSendAsync(int? year = null,int? month = null,int? instituteId = null,string? campus = null,List<int>? installmentIds = null, List<int>? commissionDetailIds = null, List<decimal>? bonusAmounts = null, List<decimal>? invoiceAmounts = null, CancellationToken cancellationToken = default)
    {
        var (targetYear, targetMonth) = ResolvePeriod(year, month);

        var result = new MonthlyInvoiceGenerateResult
        {
            Year = targetYear,
            Month = targetMonth
        };

        var paidRows = await _invoiceRepository.GetInstallmentsForMonthPreviewAsync(targetYear, targetMonth, instituteId, campus);

        if (commissionDetailIds is { Count: > 0 })
        {
            var selectedSet = commissionDetailIds.ToHashSet();
            paidRows = paidRows.Where(r => r.CommissionDetailId.HasValue && selectedSet.Contains(r.CommissionDetailId.Value)).ToList();
        }
        else if (installmentIds is { Count: > 0 })
        {
            var selectedInstallments = installmentIds.ToHashSet();
            paidRows = paidRows.Where(r => selectedInstallments.Contains(r.StudentPaymentInstallmentId)).ToList();
        }
        else
        {
            paidRows = await _invoiceRepository.GetPaidInstallmentsForMonthAsync(targetYear, targetMonth, instituteId, campus);
        }
        if (paidRows.Count == 0)
        {
            result.Message = "No received installments were found for the selected rows.";
            return result;
        }

        var dueDates = paidRows.Select(r => r.DueDate).OrderBy(d => d).ToList();
        targetYear = dueDates[0].Year;
        targetMonth = dueDates[0].Month;
        result.Year = targetYear;
        result.Month = targetMonth;

        var adminEmail = _configuration["MonthlyInvoice:AdminEmail"]
            ?? _configuration["Email:FromAddress"]
            ?? "bosco1852023@gmail.com";

        var invoiceDate = DateTime.Today;
        var groups = paidRows.GroupBy(r => r.InstituteId).OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var groupInstituteId = group.Key;
            var instituteName = group.First().InstituteName;
            var lines = group.ToList();
            // Only the ids belonging to this institute group, so the SP inserts exactly
            // what's shown in the PDF/email for this group.
            var groupInstallmentIds = lines.Select(x => x.StudentPaymentInstallmentId).ToList();
            var groupCommissionDetailIds = lines.Where(x => x.CommissionDetailId.HasValue) .Select(x => x.CommissionDetailId!.Value).Distinct().ToList();

            // Bonus Amounts
            List<decimal>? groupBonusAmounts = null;

            if (bonusAmounts is { Count: > 0 })
            {
                groupBonusAmounts = bonusAmounts;
            }
            var invoice = await _invoiceRepository.GenerateMonthlyPaidStudentInvoiceAsync(
                targetYear,
                targetMonth,
                groupInstituteId,
                campus,
                groupInstallmentIds,
                groupCommissionDetailIds,
                groupBonusAmounts,
                invoiceAmounts);
            if (invoice is null || invoice.InvoiceId <= 0)
                continue;

            var periodLabel = string.IsNullOrWhiteSpace(invoice.InvoiceMonthYear)
                ? $"{targetMonth:D2}/{targetYear}"
                : invoice.InvoiceMonthYear;
            var html = _documentService.BuildHtml(invoice, instituteName, lines, invoiceDate);
            var pdfBytes = _documentService.BuildPdfBytes(invoice, instituteName, lines, invoiceDate);
            var fileName = $"{invoice.InvoiceNumber}_{periodLabel.Replace(' ', '-').Replace(',', '-')}.pdf";

            var relativeDir = Path.Combine("uploads", "invoices");
            var absoluteDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), relativeDir);
            Directory.CreateDirectory(absoluteDir);
            var absolutePath = Path.Combine(absoluteDir, fileName);
            await File.WriteAllBytesAsync(absolutePath, pdfBytes, cancellationToken);
            await _invoiceRepository.UpdateInvoicePdfPathAsync(invoice.InvoiceId, absolutePath);

            var emailSent = false;
            try
            {
                var subject = $"Monthly Tax Invoice {invoice.InvoiceNumber} — {instituteName} ({periodLabel})";
                await _emailSender.SendWithAttachmentAsync(
                    adminEmail,
                    subject,
                    html,
                    pdfBytes,
                    fileName,
                    "application/pdf",
                    cancellationToken);
                emailSent = true;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(MonthlyInvoiceService)}.SendEmail", ex);
            }

            result.Invoices.Add(new MonthlyInvoiceResultItem
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                InstituteId = groupInstituteId,
                InstituteName = instituteName,
                TotalAmount = invoice.TotalAmount,
                StudentCount = lines.Select(x => x.StudentId).Distinct().Count(),
                DocumentPath = absolutePath,
                EmailSent = emailSent
            });
        }

        result.Message = result.Invoices.Count == 0
            ? "No invoices were created."
            : $"Created {result.Invoices.Count} invoice(s) and emailed admin ({adminEmail}).";

        return result;
    }

    private static (int Year, int Month) ResolvePeriod(int? year, int? month)
    {
        var now = DateTime.Now;
        if (year is null && month is null)
        {
            if (now.Day == 1)
                return (now.AddMonths(-1).Year, now.AddMonths(-1).Month);
            return (now.Year, now.Month);
        }

        return (year ?? now.Year, month ?? now.Month);
    }
}