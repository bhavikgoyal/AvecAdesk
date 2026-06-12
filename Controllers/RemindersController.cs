using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/reminders")]
[ApiController]
[Authorize]
public class RemindersController : ControllerBase
{
    private readonly IReminderRepository _reminderRepository;
    private readonly LogHelper _logHelper;

    public RemindersController(IReminderRepository reminderRepository, LogHelper logHelper)
    {
        _reminderRepository = reminderRepository;
        _logHelper = logHelper;
    }

    [HttpGet("rules")]
    public async Task<IActionResult> GetReminderRules()
    {
        try { return Ok(await _reminderRepository.GetReminderRulesAsync()); }
        catch (Exception ex) { _logHelper.LogError(nameof(GetReminderRules), ex); return StatusCode(500, "An error occurred while fetching reminder rules."); }
    }

    [HttpPost("rules")]
    public async Task<IActionResult> CreateReminderRule([FromBody] ReminderRuleCreateRequest request)
    {
        try
        {
            var ruleId = await _reminderRepository.CreateReminderRuleAsync(request);
            var rules = await _reminderRepository.GetReminderRulesAsync();
            return Ok(rules.FirstOrDefault(r => r.RuleId == ruleId));
        }
        catch (Exception ex) { _logHelper.LogError(nameof(CreateReminderRule), ex); return StatusCode(500, "An error occurred while creating reminder rule."); }
    }

    [HttpPut("rules/{ruleId:int}")]
    public async Task<IActionResult> UpdateReminderRule(int ruleId, [FromBody] ReminderRuleUpdateRequest request)
    {
        try
        {
            if (!await _reminderRepository.UpdateReminderRuleAsync(ruleId, request))
                return NotFound("Reminder rule not found");

            var rules = await _reminderRepository.GetReminderRulesAsync();
            return Ok(rules.FirstOrDefault(r => r.RuleId == ruleId));
        }
        catch (Exception ex) { _logHelper.LogError(nameof(UpdateReminderRule), ex); return StatusCode(500, "An error occurred while updating reminder rule."); }
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetReminderLogs()
    {
        try { return Ok(await _reminderRepository.GetReminderLogsAsync()); }
        catch (Exception ex) { _logHelper.LogError(nameof(GetReminderLogs), ex); return StatusCode(500, "An error occurred while fetching reminder logs."); }
    }

    [HttpPost("trigger/{ruleId:int}")]
    public async Task<IActionResult> TriggerReminder(int ruleId, [FromQuery] int referenceId = 0)
    {
        try
        {
            var logId = await _reminderRepository.TriggerReminderAsync(ruleId, referenceId);
            if (logId <= 0) return NotFound("Reminder rule not found");
            return Ok(new { logId, message = "Reminder triggered successfully." });
        }
        catch (Exception ex) { _logHelper.LogError(nameof(TriggerReminder), ex); return StatusCode(500, "An error occurred while triggering reminder."); }
    }
}
