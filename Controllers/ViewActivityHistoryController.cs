using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.ViewActivityHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ViewActivityHistoryController : ControllerBase
    {
        private readonly IViewActivityHistoryRepository _repo;
        private readonly ILogger<ViewActivityHistoryController> _logger;
        private readonly LogHelper _logHelper;

        public ViewActivityHistoryController(
            IViewActivityHistoryRepository repo,
            ILogger<ViewActivityHistoryController> logger,
            LogHelper logHelper)
        {
            _repo = repo;
            _logger = logger;
            _logHelper = logHelper;
        }

        [HttpGet("ViewActivityHistoryByUserId")]
        public async Task<ActionResult<List<ViewActivityHistoryResponse>>> GetActivityHistoryByUserId([FromQuery] int userId,[FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = (date ?? DateTime.UtcNow).Date;

                var result = await _repo.GetActivityHistoryByUserAsync(
                    userId,
                    targetDate);

                if (result == null || result.Count == 0)
                {
                    return Ok(new
                    {
                        message = "No tracking rows with snaps found for the given user and date.",
                        data = new List<ViewActivityHistoryResponse>()
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logHelper.LogError(
                    nameof(GetActivityHistoryByUserId),
                    ex);

                return StatusCode(500, new
                {
                    message = "Error retrieving activity history.",
                    detail = ex.Message
                });
            }
        }
    }
}