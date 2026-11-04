using AvecADeskApi.LOG;
using AvecADeskApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/commission-report-request")]
[ApiController]
[Authorize]
public class CommissionReportRequestController : ControllerBase
{
    private readonly CommissionReportRequestService _service;
    private readonly LogHelper _logHelper;

    public CommissionReportRequestController(CommissionReportRequestService service, LogHelper logHelper)
    {
        _service = service;
        _logHelper = logHelper;
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] DateTime? reportMonth)
    {
        try
        {
            return Ok(await _service.RunAsync(reportMonth, previewOnly: true));
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionReportRequestController)}.{nameof(Preview)}", ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromQuery] DateTime? reportMonth)
    {
        try
        {
            return Ok(await _service.RunAsync(reportMonth, previewOnly: false, force: true));
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionReportRequestController)}.{nameof(Run)}", ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
