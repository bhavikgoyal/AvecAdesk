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
    }
}