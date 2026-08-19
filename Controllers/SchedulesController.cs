using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.PaymentSchedule;
using AvecADeskApi.Services;
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
    private readonly InstallmentConfirmationService _confirmationService;
    private readonly LogHelper _logHelper;

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Paid", "Partial", "Pending"
    };

    public SchedulesController(
        IScheduleRepository scheduleRepository,
        IStudentRepository studentRepository,
        InstallmentConfirmationService confirmationService,
        LogHelper logHelper)
    {
        _scheduleRepository = scheduleRepository;
        _studentRepository = studentRepository;
        _confirmationService = confirmationService;
        _logHelper = logHelper;
    }
    //[HttpGet("summary")]
    //public async Task<IActionResult> GetPaymentSummary()
    //{
    //    try
    //    {
    //        var summary = await _scheduleRepository.GetPaymentSummaryAsync();
    //        var students = await _studentRepository.GetStudentsAsync(null);
    //        summary.ActiveStudents = students.Count(student => student.IsActive);
    //        return Ok(summary);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logHelper.LogError(nameof(GetPaymentSummary), ex);
    //        return StatusCode(500, "An error occurred while fetching payment summary.");
    //    }
    //}

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

    [HttpPost("CreatePaymentSchedule")]
    public async Task<IActionResult> CreatePaymentSchedule([FromBody] PaymentScheduleCreateRequest request)
    {
        try
        {
            if (request.StudentId <= 0)
                return BadRequest("Valid student ID is required");

            var scheduleId = await _scheduleRepository.CreatePaymentScheduleAsync(request);
            return Ok(await _scheduleRepository.GetPaymentScheduleByIdAsync(scheduleId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(CreatePaymentSchedule), ex);
            return StatusCode(500, "An error occurred while creating the payment schedule.");
        }
    }
    
    [HttpPost("CreateStudentPaymentInstallment")]
    public async Task<IActionResult> CreateStudentPaymentInstallment(StudentPaymentInstallmentCreateRequest request)
    {
        try
        {
            var installmentId = await _scheduleRepository.CreateStudentPaymentInstallmentAsync(request);

            return Ok(new
            {
                Success = true,
                StudentPaymentInstallmentId = installmentId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("CreateStudentCommission")]
    public async Task<IActionResult> CreateStudentCommission(StudentCommissionCreateRequest request)
    {
        try
        {
            var commissionId = await _scheduleRepository.CreateStudentCommissionAsync(request);

            return Ok(new
            {
                Success = true,
                CommissionId = commissionId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpPost("CreateStudentCommissionDetail")]
    public async Task<IActionResult> CreateStudentCommissionDetail(StudentCommissionDetailCreateRequest request)
    {
        try
        {
            var commissionDetailId = await _scheduleRepository.CreateStudentCommissionDetailAsync(request);

            return Ok(new
            {
                Success = true,
                CommissionDetailId = commissionDetailId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
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

    [HttpPost("{scheduleId:int}")]
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

    [HttpPost("{scheduleId:int}/status")]
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
    [HttpGet("GetStudentPaymentScheduleList")]
    public async Task<IActionResult> GetStudentPaymentScheduleList([FromQuery] int? studentId, [FromQuery] bool isNextMonth = false)
    {
        try
        {
            var schedules = await _scheduleRepository.GetStudentPaymentScheduleListAsync(studentId, isNextMonth);
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetStudentPaymentScheduleList), ex);
            return StatusCode(500, "An error occurred while fetching student payment schedule list.");
        }
    }
    [HttpGet("GetStudentCourseCompleteList")]
    public async Task<IActionResult> GetStudentCourseCompleteList()
    {
        try
        {
            var schedules = await _scheduleRepository.GetStudentCourseCompleteListAsync();
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetStudentCourseCompleteList), ex);
            return StatusCode(500, "An error occurred while fetching student course complete list.");
        }
    }
    [HttpPost("UpdateStudentPaymentSchedule")]
    public async Task<IActionResult> UpdateStudentPaymentSchedule([FromBody] UpdateStudentPaymentScheduleRequest request)
    {
        try
        {
            var result = await _scheduleRepository.UpdateStudentPaymentScheduleAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
   
    [HttpPost("UploadInstallmentDocument")]
    public async Task<IActionResult> UploadInstallmentDocument([FromBody] UploadInstallmentDocumentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FileBase64))
                return BadRequest("File is required.");

            var url = await _scheduleRepository.SaveInstallmentDocumentAsync(
                request.StudentPaymentInstallmentId, request.FileBase64, request.FileName);

            return Ok(new UploadInstallmentDocumentResponse { Success = true, DocumentUrl = url });
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UploadInstallmentDocument), ex);
            return StatusCode(500, "An error occurred while uploading the document.");
        }
    }

    [HttpPost("SendInstallmentConfirmationEmail")]
    public async Task<IActionResult> SendInstallmentConfirmationEmail([FromBody] SendInstallmentConfirmationEmailRequest request)
    {
        try
        {
            var (success, message) = await _confirmationService.SendConfirmationEmailAsync(request.StudentPaymentInstallmentId);
            if (!success) return BadRequest(message);
            return Ok(new SendInstallmentConfirmationEmailResponse { Success = true, Message = message });
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(SendInstallmentConfirmationEmail), ex);
            return StatusCode(500, "An error occurred while sending the confirmation email.");
        }
    }
}