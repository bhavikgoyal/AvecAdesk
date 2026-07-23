using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Receivables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/receivables")]
[ApiController]
[Authorize]
public class ReceivablesController : ControllerBase
{
    private readonly IReceivablesRepository _receivablesRepository;
    private readonly LogHelper _logHelper;

    public ReceivablesController(IReceivablesRepository receivablesRepository, LogHelper logHelper)
    {
        _receivablesRepository = receivablesRepository;
        _logHelper = logHelper;
    }

    // GET api/receivables/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] int? instituteId, [FromQuery] int? studentId)
    {
        try
        {
            var filter = BuildFilter(fromDate, toDate, instituteId, studentId);
            return Ok(await _receivablesRepository.GetSummaryAsync(filter));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetSummary), ex);
            return StatusCode(500, "An error occurred while fetching the receivables summary.");
        }
    }

    // GET api/receivables/anticipated
    [HttpGet("anticipated")]
    public async Task<IActionResult> GetAnticipated([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] int? instituteId, [FromQuery] int? studentId)
    {
        try
        {
            var filter = BuildFilter(fromDate, toDate, instituteId, studentId);
            return Ok(await _receivablesRepository.GetAnticipatedAsync(filter));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetAnticipated), ex);
            return StatusCode(500, "An error occurred while fetching anticipated receivables.");
        }
    }

    // GET api/receivables/month-revenue-dashboard
    [HttpGet("month-revenue-dashboard")]
    public async Task<IActionResult> GetMonthRevenueDashboard()
    {
        try
        {
            return Ok(await _receivablesRepository.GetMonthRevenueDashboardAsync());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetMonthRevenueDashboard), ex);
            return StatusCode(500, "An error occurred while fetching the month revenue dashboard.");
        }
    }

    [HttpGet("student-payment-installments")]
    public async Task<IActionResult> GetStudentPaymentInstallments()
    {
        try
        {
            return Ok(await _receivablesRepository.GetStudentPaymentInstallmentsAsync());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetStudentPaymentInstallments), ex);
            return StatusCode(500, "An error occurred while fetching student payment installments.");
        }
    }

    // GET api/receivables/overdue
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] int? instituteId, [FromQuery] int? studentId)
    {
        try
        {
            var filter = BuildFilter(fromDate, toDate, instituteId, studentId);
            return Ok(await _receivablesRepository.GetOverdueAsync(filter));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetOverdue), ex);
            return StatusCode(500, "An error occurred while fetching overdue receivables.");
        }
    }

    // GET api/receivables/received
    [HttpGet("received")]
    public async Task<IActionResult> GetReceived([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] int? instituteId, [FromQuery] int? studentId)
    {
        try
        {
            var filter = BuildFilter(fromDate, toDate, instituteId, studentId);
            return Ok(await _receivablesRepository.GetReceivedAsync(filter));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetReceived), ex);
            return StatusCode(500, "An error occurred while fetching received payments.");
        }
    }

    private static ReceivablesFilter BuildFilter(DateTime? fromDate, DateTime? toDate, int? instituteId, int? studentId) =>
        new()
        {
            FromDate = fromDate,
            ToDate = toDate,
            InstituteId = instituteId,
            StudentId = studentId
        };
}