using AvecADeskApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductivityController : ControllerBase
    {
        private readonly IProductivityRepository _repo;
        private readonly ILogger<ProductivityController> _logger;

        public ProductivityController(IProductivityRepository repo, ILogger<ProductivityController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int trendDays = 7)
        {
            try
            {
                var to = (toDate ?? DateTime.Today).Date;
                var from = (fromDate ?? to).Date;
                if (from > to) (from, to) = (to, from);

                var result = await _repo.GetDashboardAsync(from, to, trendDays);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Productivity dashboard error");
                return StatusCode(500, new { success = false, message = "Error fetching productivity dashboard" });
            }
        }
    }
}
