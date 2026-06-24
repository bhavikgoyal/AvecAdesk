using AvecADeskApi.DTOs.Card;
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
    public class CardController : ControllerBase
    {
        private readonly ICardRepository _repo;
        private readonly LogHelper _logHelper;

        public CardController(ICardRepository repo, LogHelper logHelper)
        {
            _repo = repo;
            _logHelper = logHelper;
        }


        [HttpGet("board")]
        public async Task<IActionResult> GetBoardCards(
            [FromQuery] string? searchText,
            [FromQuery] int? assignedUserId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var columns = await _repo.GetBoardCardsAsync(searchText, assignedUserId, fromDate, toDate);
                return Ok(columns);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardController)}.{nameof(GetBoardCards)}", ex);
                return StatusCode(500, new { message = "Error loading board", detail = ex.Message });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateCard([FromBody] CreateCardRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CardTitle))
                return BadRequest("Invalid request");

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized("Invalid token");

                var createdUserId = int.Parse(userIdClaim.Value);
                var cardId = await _repo.CreateCardAsync(request, createdUserId);
                return Ok(new { Success = true, Message = "Card created successfully", CardId = cardId });
            }
            catch (Exception ex)
            {
                _logHelper.LogError(nameof(CreateCard), ex);
                return StatusCode(500, new { message = "Error creating card", detail = ex.Message });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateCard([FromBody] UpdateCardRequest request)
        {
            if (request == null || request.CardID <= 0)
                return BadRequest("Invalid request");

            try
            {
                await _repo.UpdateCardAsync(request);
                return Ok(new { Success = true, Message = "Card updated successfully" });
            }
            catch (Exception ex)
            {
                _logHelper.LogError(nameof(UpdateCard), ex);
                return StatusCode(500, new { message = "Error updating card", detail = ex.Message });
            }
        }

        
        [HttpPatch("move")]
        public async Task<IActionResult> MoveCard([FromBody] MoveCardRequest request)
        {
            if (request == null || request.CardID <= 0)
                return BadRequest("Invalid request");

            try
            {
                await _repo.MoveCardAsync(request);
                return Ok(new { Success = true, Message = "Card moved successfully" });
            }
            catch (Exception ex)
            {
                _logHelper.LogError(nameof(MoveCard), ex);
                return StatusCode(500, new { message = "Error moving card", detail = ex.Message });
            }
        }

        [HttpDelete("delete/{cardId}")]
        public async Task<IActionResult> DeleteCard(int cardId)
        {
            if (cardId <= 0)
                return BadRequest("Invalid CardId");

            try
            {
                await _repo.DeleteCardAsync(cardId);
                return Ok(new { Success = true, Message = "Card deleted successfully" });
            }
            catch (Exception ex)
            {
                _logHelper.LogError(nameof(DeleteCard), ex);
                return StatusCode(500, new { message = "Error deleting card", detail = ex.Message });
            }
        }
    }
}
