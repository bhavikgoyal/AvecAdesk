using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.PaymentSchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/schedules")]
[ApiController]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly LogHelper _logHelper;

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Paid", "Partial", "Pending"
    };

    public SchedulesController(
        IScheduleRepository scheduleRepository,
        IStudentRepository studentRepository,
        LogHelper logHelper)
    {
        _scheduleRepository = scheduleRepository;
        _studentRepository = studentRepository;
        _logHelper = logHelper;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetPaymentSummary()
    {
        try
        {
            var summary = await _scheduleRepository.GetPaymentSummaryAsync();
            var students = await _studentRepository.GetStudentsAsync(null);
            summary.ActiveStudents = students.Count(student => student.IsActive);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetPaymentSummary), ex);
            return StatusCode(500, "An error occurred while fetching payment summary.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentSchedules([FromQuery] int? studentId)
    {
        try
        {
            return Ok(await _scheduleRepository.GetPaymentSchedulesAsync(studentId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetPaymentSchedules), ex);
            return StatusCode(500, "An error occurred while fetching payment schedules.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentSchedule([FromBody] PaymentScheduleCreateRequest request)
    {
        try
        {
            if (request.StudentId <= 0)
                return BadRequest("Valid student ID is required");

            if (request.AmountDue < 0)
                return BadRequest("Amount due must be zero or greater");

            var scheduleId = await _scheduleRepository.CreatePaymentScheduleAsync(request);
            return Ok(await _scheduleRepository.GetPaymentScheduleByIdAsync(scheduleId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(CreatePaymentSchedule), ex);
            return StatusCode(500, "An error occurred while creating the payment schedule.");
        }
    }

    [HttpPost("bulk-status")]
    public async Task<IActionResult> BulkUpdatePaymentScheduleStatus([FromBody] PaymentScheduleBulkStatusRequest request)
    {
        try
        {
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest("At least one schedule item is required");

            foreach (var item in request.Items)
            {
                if (!AllowedStatuses.Contains(item.Status))
                    return BadRequest($"Invalid status for schedule {item.ScheduleId}. Use: Paid, Partial, or Pending.");
            }

            var result = await _scheduleRepository.BulkUpdatePaymentScheduleStatusAsync(request);

            return Ok(new
            {
                result.UpdatedCount,
                result.FailedCount,
                result.Items,
                message = result.FailedCount == 0
                    ? $"{result.UpdatedCount} schedule(s) updated."
                    : $"{result.UpdatedCount} updated, {result.FailedCount} failed. See 'items' for details."
            });
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(BulkUpdatePaymentScheduleStatus), ex);
            return StatusCode(500, "An error occurred while bulk updating payment schedules.");
        }
    }

    [HttpPut("{scheduleId:int}")]
    public async Task<IActionResult> UpdatePaymentSchedule(int scheduleId, [FromBody] PaymentScheduleUpdateRequest request)
    {
        try
        {
            var updated = await _scheduleRepository.UpdatePaymentScheduleAsync(scheduleId, request);
            if (!updated)
                return NotFound("Payment schedule not found");

            return Ok(await _scheduleRepository.GetPaymentScheduleByIdAsync(scheduleId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdatePaymentSchedule), ex);
            return StatusCode(500, "An error occurred while updating the payment schedule.");
        }
    }

    [HttpPut("{scheduleId:int}/status")]
    public async Task<IActionResult> UpdatePaymentScheduleStatus(int scheduleId, [FromBody] PaymentScheduleStatusRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Status) || !AllowedStatuses.Contains(request.Status))
                return BadRequest("Invalid status. Use: Paid, Partial, or Pending.");

            var updated = await _scheduleRepository.UpdatePaymentScheduleStatusAsync(
                scheduleId, request.Status, request.AmountPaid);

            if (!updated)
                return NotFound("Payment schedule not found");

            return Ok(await _scheduleRepository.GetPaymentScheduleByIdAsync(scheduleId));
        }
        catch (InvalidOperationException ex)
        {
           
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdatePaymentScheduleStatus), ex);
            return StatusCode(500, "An error occurred while updating payment schedule status.");
        }
    }
}