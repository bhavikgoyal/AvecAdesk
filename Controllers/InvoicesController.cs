using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Invoice;
using AvecADeskApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using System.Text;

namespace AvecADeskApi.Controllers;

[Route("api/invoices")]
[ApiController]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly MonthlyInvoiceService _monthlyInvoiceService;
    private readonly LogHelper _logHelper;

    public InvoicesController(
        IInvoiceRepository invoiceRepository,
        MonthlyInvoiceService monthlyInvoiceService,
        LogHelper logHelper)
    {
        _invoiceRepository = invoiceRepository;
        _monthlyInvoiceService = monthlyInvoiceService;
        _logHelper = logHelper;
    }

    /// <summary>
    /// Preview all student installments due in the month (any status), filtered by institute + campus.
    /// Invoice generation still uses Paid rows only.
    /// </summary>
    [HttpGet("paid-students")]
    public async Task<IActionResult> GetPaidStudentsForInvoice(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int? instituteId,
        [FromQuery] string? campus)
    {
        try
        {
            var rows = await _monthlyInvoiceService.GetPaidStudentsAsync(year, month, instituteId, campus);
            return Ok(rows);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetPaidStudentsForInvoice), ex);
            return StatusCode(500, "An error occurred while fetching students.");
        }
    }

    /// <summary>
    /// Generate monthly tax invoice(s) for students with Paid installments in the given month,
    /// insert Invoices + InvoiceItem rows, and email admin (bosco1852023@gmail.com by default).
    /// </summary>
    [HttpPost("generate-monthly")]
    public async Task<IActionResult> GenerateMonthlyInvoice([FromBody] MonthlyInvoiceGenerateRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _monthlyInvoiceService.GenerateAndSendAsync(
                request?.Year,
                request?.Month,
                request?.InstituteId,
                request?.Campus,
                cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GenerateMonthlyInvoice), ex);
            return StatusCode(500, "An error occurred while generating the monthly invoice.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices()
    {
        try { return Ok(await _invoiceRepository.GetInvoicesAsync()); }
        catch (Exception ex) { _logHelper.LogError(nameof(GetInvoices), ex); return StatusCode(500, "An error occurred while fetching invoices."); }
    }

    [HttpPost("generate/{uploadId:int}")]
    public async Task<IActionResult> GenerateInvoice(int uploadId)
    {
        try
        {
            var invoiceId = await _invoiceRepository.GenerateInvoiceAsync(uploadId);
            if (invoiceId <= 0) return NotFound("Upload not found or cannot generate invoice");
            return Ok(await _invoiceRepository.GetInvoiceByIdAsync(invoiceId));
        }
        catch (Exception ex) { _logHelper.LogError(nameof(GenerateInvoice), ex); return StatusCode(500, "An error occurred while generating invoice."); }
    }

    [HttpGet("{invoiceId:int}")]
    public async Task<IActionResult> GetInvoiceById(int invoiceId)
    {
        try
        {
            var invoice = await _invoiceRepository.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) return NotFound("Invoice not found");
            return Ok(invoice);
        }
        catch (Exception ex) { _logHelper.LogError(nameof(GetInvoiceById), ex); return StatusCode(500, "An error occurred while fetching invoice."); }
    }
    [HttpGet("{invoiceId:int}/line-items")]
    public async Task<IActionResult> GetInvoiceLineItems(int invoiceId)
    {
        try
        {
            var lineItems = await _invoiceRepository.GetInvoiceLineItemsAsync(invoiceId);
            return Ok(lineItems);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetInvoiceLineItems), ex);
            return StatusCode(500, "An error occurred while fetching invoice line items.");
        }
    }
    [HttpPut("{invoiceId:int}/submit")]
    public async Task<IActionResult> SubmitInvoice(int invoiceId)
    {
        try
        {
            if (!await _invoiceRepository.SubmitInvoiceAsync(invoiceId)) return NotFound("Invoice not found");
            return Ok(await _invoiceRepository.GetInvoiceByIdAsync(invoiceId));
        }
        catch (Exception ex) { _logHelper.LogError(nameof(SubmitInvoice), ex); return StatusCode(500, "An error occurred while submitting invoice."); }
    }

    [HttpPut("{invoiceId:int}/approve")]
    public async Task<IActionResult> ApproveInvoice(int invoiceId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var invoice = await _invoiceRepository.ApproveInvoiceAsync(invoiceId, userId);
            if (invoice == null) return NotFound("Invoice not found or cannot be approved");
            return Ok(invoice);
        }
        catch (Exception ex) { _logHelper.LogError(nameof(ApproveInvoice), ex); return StatusCode(500, "An error occurred while approving invoice."); }
    }

    [HttpPut("{invoiceId:int}/reject")]
    public async Task<IActionResult> RejectInvoice(int invoiceId, [FromBody] InvoiceRejectRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                return BadRequest("Rejection reason is required");

            if (!await _invoiceRepository.RejectInvoiceAsync(invoiceId, request.RejectionReason))
                return NotFound("Invoice not found");

            return Ok(await _invoiceRepository.GetInvoiceByIdAsync(invoiceId));
        }
        catch (Exception ex) { _logHelper.LogError(nameof(RejectInvoice), ex); return StatusCode(500, "An error occurred while rejecting invoice."); }
    }

    //[HttpGet("{invoiceId:int}/pdf")]
    //public async Task<IActionResult> GetInvoicePdf(int invoiceId)
    //{
    //    try
    //    {
    //        var invoice = await _invoiceRepository.GetInvoiceByIdAsync(invoiceId);
    //        if (invoice == null) return NotFound("Invoice not found");

    //        var lines = await _invoiceRepository.GetInvoiceLineItemsAsync(invoiceId);
    //        var sb = new StringBuilder();
    //        sb.AppendLine("TAX INVOICE");
    //        sb.AppendLine("===========");
    //        sb.AppendLine($"Invoice No: {invoice.InvoiceNumber}");
    //        sb.AppendLine($"Institute: {invoice.InstituteName}");
    //        sb.AppendLine($"Status: {invoice.Status}");
    //        sb.AppendLine($"Created At: {invoice.CreatedAt:dd-MM-yyyy HH:mm}");
    //        sb.AppendLine($"Total Amount (AUD): {invoice.TotalAmount:0.00}");
    //        sb.AppendLine();
    //        sb.AppendLine("Line Items");
    //        sb.AppendLine("----------");

    //        if (lines.Count == 0)
    //        {
    //            sb.AppendLine("No line items.");
    //        }
    //        else
    //        {
    //            var sr = 1;
    //            foreach (var line in lines)
    //            {
    //                sb.AppendLine($"{sr}. {line.Description}");
    //                sb.AppendLine($"   Amount (AUD): {line.Amount:0.00}");
    //                sb.AppendLine();
    //                sr++;
    //            }
    //        }

    //        sb.AppendLine("Bank Details");
    //        sb.AppendLine("------------");
    //        sb.AppendLine("Account Name: AVEC GLOBAL GROUP PTY LTD");
    //        sb.AppendLine("BSB: 063-549");
    //        sb.AppendLine("Account Number: 1081 0692");
    //        sb.AppendLine("Address: Unit 3, 380 Clayton Road, Clayton, Vic: 3168");

    //        var fileName = $"{invoice.InvoiceNumber}.txt";
    //        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/plain", fileName);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logHelper.LogError(nameof(GetInvoicePdf), ex);
    //        return StatusCode(500, "An error occurred while downloading invoice.");
    //    }
    //}
    [HttpGet("{invoiceId:int}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(int invoiceId)
    {
        try
        {
            var invoice = await _invoiceRepository.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) return NotFound("Invoice not found");

            var lines = await _invoiceRepository.GetInvoiceLineItemsAsync(invoiceId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("TAX INVOICE").FontSize(20).Bold();
                        col.Item().PaddingTop(10).Text($"Invoice No: {invoice.InvoiceNumber}");
                        col.Item().Text($"Institute: {invoice.InstituteName}");
                        col.Item().Text($"Status: {invoice.Status}");
                        col.Item().Text($"Created At: {invoice.CreatedAt:dd-MM-yyyy HH:mm}");
                        col.Item().Text($"Total Amount (AUD): {invoice.TotalAmount:0.00}").Bold();
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Text("Line Items").Bold().FontSize(13);

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("#").Bold();
                                header.Cell().Text("Student").Bold();
                                header.Cell().Text("Description").Bold();
                                header.Cell().Text("Amount (AUD)").Bold();
                            });

                            var sr = 1;
                            if (lines.Count == 0)
                            {
                                table.Cell().ColumnSpan(4).Text("No line items.");
                            }
                            else
                            {
                                foreach (var line in lines)
                                {
                                    table.Cell().Text(sr.ToString());
                                    table.Cell().Text(line.StudentName ?? "—");
                                    table.Cell().Text(line.Description ?? "—");
                                    table.Cell().Text(line.Amount.ToString("0.00"));
                                    sr++;
                                }
                            }
                        });

                        col.Item().PaddingTop(20).Text("Bank Details").Bold().FontSize(13);
                        col.Item().Text("Account Name: AVEC GLOBAL GROUP PTY LTD");
                        col.Item().Text("BSB: 063-549");
                        col.Item().Text("Account Number: 1081 0692");
                        col.Item().Text("Address: Unit 3, 380 Clayton Road, Clayton, Vic: 3168");
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated by AiDesk").FontSize(9);
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var fileName = $"{invoice.InvoiceNumber}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetInvoicePdf), ex);
            return StatusCode(500, "An error occurred while downloading invoice.");
        }
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
