using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace AvecADeskApi.Controllers;

[Route("api/invoices")]
[ApiController]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly LogHelper _logHelper;

    public InvoicesController(IInvoiceRepository invoiceRepository, LogHelper logHelper)
    {
        _invoiceRepository = invoiceRepository;
        _logHelper = logHelper;
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

    [HttpGet("{invoiceId:int}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(int invoiceId)
    {
        try
        {
            var invoice = await _invoiceRepository.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) return NotFound("Invoice not found");

            if (!string.IsNullOrEmpty(invoice.PdfPath) && System.IO.File.Exists(invoice.PdfPath))
                return File(await System.IO.File.ReadAllBytesAsync(invoice.PdfPath), "application/pdf", Path.GetFileName(invoice.PdfPath));

            var text = $"Invoice: {invoice.InvoiceNumber}\nAmount: {invoice.TotalAmount}\nStatus: {invoice.Status}";
            return File(Encoding.UTF8.GetBytes(text), "text/plain", $"invoice-{invoice.InvoiceNumber}.txt");
        }
        catch (Exception ex) { _logHelper.LogError(nameof(GetInvoicePdf), ex); return StatusCode(500, "An error occurred while downloading invoice PDF."); }
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
