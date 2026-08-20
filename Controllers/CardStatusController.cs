using AvecADeskApi.Interfaces; 
using AvecADeskApi.LOG;       
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CardStatusController : ControllerBase
    {
        private readonly ICardStatusRepository _repo;
        private readonly LogHelper _logHelper;

        public CardStatusController(ICardStatusRepository repo, LogHelper logHelper)
        {
            _repo = repo;
            _logHelper = logHelper;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetCardStatuses()
        {
            try
            {
                var statuses = await _repo.GetCardStatusesAsync();
                return Ok(statuses);
            }
            catch (Exception ex)
            {
               
                _logHelper.LogError($"{nameof(CardStatusController)}.{nameof(GetCardStatuses)}", ex);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching card statuses." }
                );
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateCardStatus([FromBody] CreateCardStatusRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.StatusName))
                return BadRequest(new { message = "List name is required." });

            try
            {
                var status = await _repo.CreateCardStatusAsync(request.StatusName);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardStatusController)}.{nameof(CreateCardStatus)}", ex);
                return StatusCode(500, new { message = "An error occurred while creating the list." });
            }
        }
    }

    public class CreateCardStatusRequest
    {
        public string StatusName { get; set; } = string.Empty;
    }
}