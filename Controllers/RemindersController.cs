using System.Security.Claims;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;
using AvecADeskApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/reminders")]
[ApiController]
[Authorize]
public class RemindersController : ControllerBase
{
    private readonly IReminderRepository _reminderRepository;
    private readonly AccountingPaymentReminderService _accountingPaymentReminderService;
    private readonly ContractExpiryReminderService _contractExpiryReminderService;
    private readonly InvoiceDueReminderService _invoiceDueReminderService;
    private readonly IStudentFeeTodoReminderRepository _studentFeeTodoReminderRepository;
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;

    public RemindersController(
        IReminderRepository reminderRepository,
        AccountingPaymentReminderService accountingPaymentReminderService,
        ContractExpiryReminderService contractExpiryReminderService,
         InvoiceDueReminderService invoiceDueReminderService,
        IStudentFeeTodoReminderRepository studentFeeTodoReminderRepository,
        IConfiguration configuration,
        LogHelper logHelper)
    {
        _reminderRepository = reminderRepository;
        _accountingPaymentReminderService = accountingPaymentReminderService;
        _contractExpiryReminderService = contractExpiryReminderService;
        _invoiceDueReminderService = invoiceDueReminderService;
        _studentFeeTodoReminderRepository = studentFeeTodoReminderRepository;
        _configuration = configuration;
        _logHelper = logHelper;
    }

    [HttpPost("dues-todo/dismiss")]
    public async Task<IActionResult> DismissDuesTodo([FromBody] DismissDuesTodoRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ItemName) || request.CardDate == null)
            return BadRequest("ItemName and CardDate are required.");

        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
        try
        {
            await _studentFeeTodoReminderRepository.DismissDuesTodoItemAsync(request.ItemName, request.CardDate.Value, userId);
            return Ok(new { Success = true });
        }
        catch (Exception ex) { _logHelper.LogError(nameof(DismissDuesTodo), ex); return StatusCode(500, "An error occurred while hiding the task."); }
    }

    [HttpGet("dues-todo")]
    public async Task<IActionResult> GetDuesTodo([FromQuery] string? scope = null)
    {
        var daysBefore = _configuration.GetValue("StudentFeeTodoReminder:DaysBefore", 7);
        var normalizedScope = string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase) ? "all" : "previous";
        try { return Ok(await _studentFeeTodoReminderRepository.GetDuesTodoItemsAsync(daysBefore, normalizedScope)); }
        catch (Exception ex) { _logHelper.LogError(nameof(GetDuesTodo), ex); return StatusCode(500, "An error occurred while fetching dues to do."); }
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

    [HttpPost("rules/{ruleId:int}")]
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
    [HttpGet("stats")]
    public async Task<IActionResult> GetReminderStats()
    {
        try
        {
            
            var stats = await _reminderRepository.GetReminderStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetReminderStats), ex);
            return StatusCode(500, "An error occurred while fetching reminder stats.");
        }
    }

    [HttpGet("accounting-payment-tasks/preview")]
    public async Task<IActionResult> PreviewAccountingPaymentTasks([FromQuery] int? daysBefore = null)
    {
        try
        {
            var result = await _accountingPaymentReminderService.PreviewAsync(daysBefore);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(PreviewAccountingPaymentTasks), ex);
            return StatusCode(500, "An error occurred while previewing accounting payment reminder tasks.");
        }
    }

    [HttpPost("accounting-payment-tasks/run")]
    public async Task<IActionResult> RunAccountingPaymentTasks([FromQuery] int? daysBefore = null)
    {
        try
        {
            var result = await _accountingPaymentReminderService.RunAsync(daysBefore);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(RunAccountingPaymentTasks), ex);
            return StatusCode(500, "An error occurred while creating accounting payment reminder tasks.");
        }
    }
    [HttpGet("contract-expiry-tasks/preview")]
    public async Task<IActionResult> PreviewContractExpiryTasks([FromQuery] int? daysBefore = null)
    {
        try
        {
            var result = await _contractExpiryReminderService.PreviewAsync(daysBefore);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(PreviewContractExpiryTasks), ex);
            return StatusCode(500, "An error occurred while previewing contract expiry tasks.");
        }
    }

    [HttpPost("contract-expiry-tasks/run")]
    public async Task<IActionResult> RunContractExpiryTasks([FromQuery] int? daysBefore = null)
    {
        try
        {
            var result = await _contractExpiryReminderService.RunAsync(daysBefore);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(RunContractExpiryTasks), ex);
            return StatusCode(500, "An error occurred while creating contract expiry tasks.");
        }
    }

    [HttpGet("invoice-due-tasks/preview")]
    public async Task<IActionResult> PreviewInvoiceDueTasks()
    {
        try
        {
            var result = await _invoiceDueReminderService.PreviewAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(PreviewInvoiceDueTasks), ex);
            return StatusCode(500, "An error occurred while previewing invoice due tasks.");
        }
    }

    [HttpPost("invoice-due-tasks/run")]
    public async Task<IActionResult> RunInvoiceDueTasks()
    {
        try
        {
            var result = await _invoiceDueReminderService.RunAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(RunInvoiceDueTasks), ex);
            return StatusCode(500, "An error occurred while creating invoice due tasks.");
        }
    }

}
