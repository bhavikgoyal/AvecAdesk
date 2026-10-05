using AvecADeskApi.DTOs.Card;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Services.Trello;
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
        private readonly TrelloSyncQueue _trelloQueue;

        public CardController(ICardRepository repo, LogHelper logHelper, TrelloSyncQueue trelloQueue)
        {
            _repo = repo;
            _logHelper = logHelper;
            _trelloQueue = trelloQueue;
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

        [HttpGet("board/{boardId:int}")]
        public async Task<IActionResult> GetCardsByBoardId(
    int boardId,
    [FromQuery] string? searchText,
    [FromQuery] int? assignedUserId,
    [FromQuery] DateTime? fromDate,
    [FromQuery] DateTime? toDate)
        {
            try
            {
                if (boardId <= 0)
                    return BadRequest("Invalid BoardID.");

                var cards = await _repo.GetCardsByBoardIdAsync(
                    boardId,
                    searchText,
                    assignedUserId,
                    fromDate,
                    toDate);

                return Ok(cards);
            }
            catch (Exception ex)
            {
                _logHelper.LogError(
                    $"{nameof(CardController)}.{nameof(GetCardsByBoardId)}",
                    ex
                );

                return StatusCode(
                    500,
                    new
                    {
                        message = "Error loading board cards",
                        detail = ex.Message
                    }
                );
            }
        }

        [HttpGet("my-board")]
        public async Task<IActionResult> GetMyAssignedBoardCards(
            [FromQuery] string? searchText,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized("Invalid token");

                var assignedUserId = int.Parse(userIdClaim.Value);
                var columns = await _repo.GetMyAssignedBoardCardsAsync(
                    assignedUserId,
                    searchText,
                    fromDate,
                    toDate);
                return Ok(columns);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardController)}.{nameof(GetMyAssignedBoardCards)}", ex);
                return StatusCode(500, new { message = "Error loading assigned board", detail = ex.Message });
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
                _trelloQueue.LocalCardChanged(cardId);
                return Ok(new { Success = true, Message = "Card created successfully", CardId = cardId });
            }
            catch (Exception ex)
            {
                _logHelper.LogError(nameof(CreateCard), ex);
                return StatusCode(500, new { message = "Error creating card", detail = ex.Message });
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateCard([FromBody] UpdateCardRequest request)
        {
            if (request == null || request.CardID <= 0)
                return BadRequest("Invalid request");

            try
            {
                await _repo.UpdateCardAsync(request);
                _trelloQueue.LocalCardChanged(request.CardID);
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

        [HttpPost("move-to-list")]
        public async Task<IActionResult> MoveCardToList([FromBody] MoveCardToListRequest request)
        {
            if (request == null || request.CardID <= 0 || request.ListID <= 0)
                return BadRequest(new { message = "CardID and ListID are required." });

            try
            {
                var result = await _repo.MoveCardToListAsync(request);
                _trelloQueue.LocalCardChanged(request.CardID);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logHelper.LogError(nameof(MoveCardToList), ex);
                return StatusCode(500, new { message = "Error moving card", detail = ex.Message });
            }
        }

        [HttpPost("delete/{cardId}")]
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
