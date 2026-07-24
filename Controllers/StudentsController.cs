using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/students")]
[ApiController]
public class StudentsController : ControllerBase
{
    private readonly IStudentRepository _studentRepository;
    private readonly LogHelper _logHelper;

    private static readonly HashSet<string> AllowedEnrolmentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Interested", "Applied", "Enrolled", "Dropped"
    };

    public StudentsController(IStudentRepository studentRepository, LogHelper logHelper)
    {
        _studentRepository = studentRepository;
        _logHelper = logHelper;
    }

    [Authorize]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllStudents()
    {
        try
        {
            return Ok(await _studentRepository.GetAllStudentsAsync());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetAllStudents), ex);
            return StatusCode(500, "An error occurred while fetching all students.");
        }
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetStudents([FromQuery] string? enrolmentStatus)
    {
        try
        {
            return Ok(await _studentRepository.GetStudentsAsync(
                string.IsNullOrWhiteSpace(enrolmentStatus) ? null : enrolmentStatus.Trim()));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetStudents), ex);
            return StatusCode(500, "An error occurred while fetching students.");
        }
    }

    [Authorize]
    [HttpGet("{studentId:int}")]
    public async Task<IActionResult> GetStudentById(int studentId)
    {
        try
        {
            var student = await _studentRepository.GetStudentByIdAsync(studentId);
            if (student == null)
                return NotFound("Student not found");

            return Ok(student);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetStudentById), ex);
            return StatusCode(500, "An error occurred while fetching the student.");
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateStudent([FromBody] StudentCreateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest("Full name is required");

            if (string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest("Phone is required");

            var studentId = await _studentRepository.CreateStudentAsync(request);
            return Ok(await _studentRepository.GetStudentByIdAsync(studentId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(CreateStudent), ex);
            return StatusCode(500, "An error occurred while creating the student.");
        }
    }

    [Authorize]
    [HttpPut("{studentId:int}")]
    public async Task<IActionResult> UpdateStudent(int studentId, [FromBody] StudentUpdateRequest request)
    {
        try
        {
            var updated = await _studentRepository.UpdateStudentAsync(studentId, request);
            if (!updated)
                return NotFound("Student not found");

            return Ok(await _studentRepository.GetStudentByIdAsync(studentId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateStudent), ex);
            return StatusCode(500, "An error occurred while updating the student.");
        }
    }

    [Authorize]
    [HttpPut("{studentId:int}/enrolment-status")]
    public async Task<IActionResult> UpdateEnrolmentStatus(int studentId, [FromBody] StudentEnrolmentStatusRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.EnrolmentStatus) || !AllowedEnrolmentStatuses.Contains(request.EnrolmentStatus))
                return BadRequest("Invalid enrolment status. Use: Interested, Applied, Enrolled, Dropped.");

            var updated = await _studentRepository.UpdateStudentEnrolmentStatusAsync(studentId, request.EnrolmentStatus);
            if (!updated)
                return NotFound("Student not found");

            return Ok(await _studentRepository.GetStudentByIdAsync(studentId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateEnrolmentStatus), ex);
            return StatusCode(500, "An error occurred while updating enrolment status.");
        }
    }
    [HttpGet("GetStudentPaymentDetail/{studentId}")]
    public async Task<IActionResult> GetStudentPaymentDetail(int studentId)
    {
        try
        {
            var result = await _studentRepository.GetStudentPaymentDetailByIdAsync(studentId);

            if (result == null)
                return NotFound(new { Message = "Student not found." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Message = ex.Message
            });
        }
    }
}
