using AvecADeskApi.DTOs.Label;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LabelController : ControllerBase
    {
        private readonly ILabelRepository _repo;
        private readonly LogHelper _logHelper;

        public LabelController(ILabelRepository repo, LogHelper logHelper)
        {
            _repo = repo;
            _logHelper = logHelper;
        }

        [HttpGet("card/{cardId:int}")]
        public async Task<IActionResult> GetByCard(int cardId)
        {
            if (cardId <= 0) return BadRequest(new { message = "Valid CardID is required." });

            try
            {
                var labels = await _repo.GetByCardIdAsync(cardId);
                return Ok(labels);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelController)}.{nameof(GetByCard)}", ex);
                return StatusCode(500, new { message = "Error loading labels", detail = ex.Message });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateLabelRequest request)
        {
            if (request == null || request.CardID <= 0)
                return BadRequest(new { message = "Valid CardID is required." });

            if (string.IsNullOrWhiteSpace(request.LabelName))
                return BadRequest(new { message = "Label name is required." });

            try
            {
                var created = await _repo.CreateLabelAsync(new CreateLabelRequest
                {
                    CardID = request.CardID,
                    LabelName = request.LabelName.Trim(),
                    Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim(),
                });

                return Ok(created);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelController)}.{nameof(Create)}", ex);
                return StatusCode(500, new { message = "Error creating label", detail = ex.Message });
            }
        }

        [HttpDelete("delete/{labelId:int}")]
        public async Task<IActionResult> Delete(int labelId)
        {
            if (labelId <= 0) return BadRequest(new { message = "Valid LabelID is required." });

            try
            {
                var deleted = await _repo.DeleteLabelAsync(labelId);
                if (!deleted) return NotFound(new { message = "Label not found." });
                return Ok(new { message = "Label deleted successfully." });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelController)}.{nameof(Delete)}", ex);
                return StatusCode(500, new { message = "Error deleting label", detail = ex.Message });
            }
        }
    }
}
