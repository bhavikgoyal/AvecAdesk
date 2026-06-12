using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Course;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AvecADeskApi.Controllers;

[Route("api/courses")]
[ApiController]
public class CoursesController : ControllerBase
{
    private readonly ICourseRepository _courseRepository;
    private readonly LogHelper _logHelper;

    public CoursesController(ICourseRepository courseRepository, LogHelper logHelper)
    {
        _courseRepository = courseRepository;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses([FromQuery] int? instituteId)
    {
        try
        {
            return Ok(await _courseRepository.GetCoursesByInstituteAsync(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetCourses), ex);
            return StatusCode(500, "An error occurred while fetching courses.");
        }
    }

    [HttpGet("{courseId:int}")]
    public async Task<IActionResult> GetCourseById(int courseId)
    {
        try
        {
            var course = await _courseRepository.GetCourseByIdAsync(courseId);
            if (course == null)
                return NotFound("Course not found");

            return Ok(course);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetCourseById), ex);
            return StatusCode(500, "An error occurred while fetching the course.");
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CourseCreateRequest request)
    {
        try
        {
            if (request.InstituteId <= 0)
                return BadRequest("Valid institute ID is required");

            if (string.IsNullOrWhiteSpace(request.CourseName))
                return BadRequest("Course name is required");

            var courseId = await _courseRepository.CreateCourseAsync(request);
            var created = await _courseRepository.GetCourseByIdAsync(courseId);
            return Ok(created);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(CreateCourse), ex);
            return StatusCode(500, "An error occurred while creating the course.");
        }
    }

    [Authorize]
    [HttpPut("{courseId:int}")]
    public async Task<IActionResult> UpdateCourse(int courseId, [FromBody] CourseUpdateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CourseName))
                return BadRequest("Course name is required");

            var updated = await _courseRepository.UpdateCourseAsync(courseId, request);
            if (!updated)
                return NotFound("Course not found");

            return Ok(await _courseRepository.GetCourseByIdAsync(courseId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateCourse), ex);
            return StatusCode(500, "An error occurred while updating the course.");
        }
    }

    [Authorize]
    [HttpPut("{courseId:int}/approve")]
    public async Task<IActionResult> ApproveCourse(int courseId)
    {
        try
        {
            var userId = 15;//GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token. Please login again to get a new token.");

            var course = await _courseRepository.ApproveCourseAsync(courseId, userId);
            if (course == null)
                return NotFound("Course not found or cannot be approved");

            return Ok(course);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(ApproveCourse), ex);
            return StatusCode(500, "An error occurred while approving the course.");
        }
    }

    [Authorize]
    [HttpDelete("{courseId:int}")]
    public async Task<IActionResult> DeleteCourse(int courseId)
    {
        try
        {
            var deleted = await _courseRepository.DeleteCourseAsync(courseId);
            if (!deleted)
                return NotFound("Course not found");

            return Ok(new { message = "Course deleted successfully." });
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(DeleteCourse), ex);
            return StatusCode(500, "An error occurred while deleting the course.");
        }
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;

        return null;
    }
}
