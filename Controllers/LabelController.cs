using AvecADeskApi.DTOs.Label;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        [HttpGet("board/card/{cardId:int}")]
        public async Task<IActionResult> GetBoardLabelsForCard(int cardId)
        {
            if (cardId <= 0) return BadRequest(new { message = "Valid CardID is required." });

            try
            {
                var labels = await _repo.GetBoardLabelsForCardAsync(cardId);
                return Ok(labels);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelController)}.{nameof(GetBoardLabelsForCard)}", ex);
                return StatusCode(500, new { message = "Error loading board labels", detail = ex.Message });
            }
        }

        [HttpPost("board/create")]
        public async Task<IActionResult> CreateBoardLabel([FromBody] CreateBoardLabelRequest request)
        {
            if (request == null || request.CardID <= 0)
                return BadRequest(new { message = "Valid CardID is required." });

            request.LabelName = request.LabelName?.Trim() ?? string.Empty;
            request.Color = request.Color?.Trim() ?? string.Empty;

            if (request.LabelName.Length == 0 && request.Color.Length == 0)
                return BadRequest(new { message = "Label name or color is required." });

            try
            {
                var created = await _repo.CreateBoardLabelAsync(request, GetCurrentUserId());
                return Ok(created);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelController)}.{nameof(CreateBoardLabel)}", ex);
                return StatusCode(500, new { message = "Error creating label", detail = ex.Message });
            }
        }

        [HttpPost("board/update/{boardLabelId:int}")]
        public async Task<IActionResult> UpdateBoardLabel(int boardLabelId, [FromBody] UpdateBoardLabelRequest request)
        {
            if (boardLabelId <= 0 || request == null)
                return BadRequest(new { message = "Valid BoardLabelID is required." });

            request.LabelName = request.LabelName?.Trim() ?? string.Empty;
            request.Color = request.Color?.Trim() ?? string.Empty;

            if (request.LabelName.Length == 0 && request.Color.Length == 0)
                return BadRequest(new { message = "Label name or color is required." });

            try
            {
                var updated = await _repo.UpdateBoardLabelAsync(boardLabelId, request);
                if (updated == null) return NotFound(new { message = "Label not found." });
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelController)}.{nameof(UpdateBoardLabel)}", ex);
                return StatusCode(500, new { message = "Error updating label", detail = ex.Message });
            }
        }

        [HttpPost("board/delete/{boardLabelId:int}")]
        public async Task<IActionResult> DeleteBoardLabel(int boardLabelId)
        {
            if (boardLabelId <= 0) return BadRequest(new { message = "Valid BoardLabelID is required." });

            try
            {
                var deleted = await _repo.DeleteBoardLabelAsync(boardLabelId);
                if (!deleted) return NotFound(new { message = "Label not found." });
                return Ok(new { message = "Label deleted successfully." });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelController)}.{nameof(DeleteBoardLabel)}", ex);
                return StatusCode(500, new { message = "Error deleting label", detail = ex.Message });
            }
        }

        [HttpPost("board/toggle")]
        public async Task<IActionResult> SetCardBoardLabel([FromBody] SetCardBoardLabelRequest request)
        {
            if (request == null || request.CardID <= 0 || request.BoardLabelID <= 0)
                return BadRequest(new { message = "Valid CardID and BoardLabelID are required." });

            try
            {
                var label = await _repo.SetCardBoardLabelAsync(request);
                return Ok(label);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelController)}.{nameof(SetCardBoardLabel)}", ex);
                return StatusCode(500, new { message = "Error updating card label", detail = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        [HttpPost("delete/{labelId:int}")]
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
