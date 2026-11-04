using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.MailHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/mail-history")]
[ApiController]
[Authorize(Roles = "Admin,Super Admin")]
public class MailHistoryController : ControllerBase
{
    private readonly IMailHistoryRepository _repository;
    private readonly LogHelper _logHelper;

    public MailHistoryController(IMailHistoryRepository repository, LogHelper logHelper)
    {
        _repository = repository;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetMailHistory([FromQuery] MailHistoryQuery query)
    {
        try
        {
            return Ok(await _repository.GetPagedAsync(query));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetMailHistory), ex);
            return StatusCode(500, "An error occurred while fetching mail history.");
        }
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetMailTypes()
    {
        try
        {
            return Ok(await _repository.GetMailTypesAsync());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetMailTypes), ex);
            return StatusCode(500, "An error occurred while fetching mail types.");
        }
    }

    [HttpGet("{mailHistoryId:int}")]
    public async Task<IActionResult> GetMailHistoryById(int mailHistoryId)
    {
        try
        {
            var detail = await _repository.GetByIdAsync(mailHistoryId);
            return detail == null ? NotFound("Mail not found.") : Ok(detail);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetMailHistoryById), ex);
            return StatusCode(500, "An error occurred while fetching the mail.");
        }
    }
}
