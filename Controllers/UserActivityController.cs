using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.UserActivity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserActivityController : ControllerBase
    {
        private readonly IUserActivityRepository _repo;
        private readonly ILogger<UserActivityController> _logger;

        public UserActivityController( IUserActivityRepository repo,ILogger<UserActivityController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet("daily")]
        public async Task<IActionResult> GetDailyReport([FromQuery] DateTime fromDate,[FromQuery] DateTime toDate,string? employeeName = null)
        {
            try
            {
                var result = await _repo.GetWorkReportAsync(fromDate, toDate, employeeName);

                if (result == null || !result.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Data not available",
                        data = new List<UserActivityResponse>()
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily Report Error");

                return Ok(new
                {
                    success = false,
                    message = "Data not available",
                    data = new List<UserActivityResponse>()
                });
            }
        }

        [HttpGet("weekly")]
        public async Task<IActionResult> GetWeeklyReport([FromQuery] DateTime fromDate,[FromQuery] DateTime toDate,[FromQuery] string? employeeName = null)
        {
            try
            {
             var result = await _repo.GetWorkReportAsync(fromDate,toDate,employeeName);
                if (result == null || !result.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Data not available",
                        data = new List<UserActivityResponse>()
                    });
                }
                return Ok(new
                {
                    success = true,
                    message = "Weekly report fetched successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weekly report");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching weekly report"
                });
            }
        }

        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthlyReport([FromQuery] DateTime fromDate,[FromQuery] DateTime toDate,[FromQuery] string? employeeName = null)
        {
            try
            {
                var result = await _repo.GetWorkReportAsync(fromDate,toDate,employeeName);

                if (result == null || !result.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Data not available",
                        data = new List<UserActivityResponse>()
                    });
                }
                return Ok(new
                {
                    success = true,
                    message = "Monthly report fetched successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching monthly report");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching monthly report"
                });
            }
        }
    }
}