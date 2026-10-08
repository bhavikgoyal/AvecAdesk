using AvecADeskApi.DTOs.Board;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Services.Trello;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BoardController : ControllerBase
    {
        private readonly IBoardRepository _boardRepository;
        private readonly TrelloSyncQueue _trelloQueue;

        public BoardController(IBoardRepository boardRepository, TrelloSyncQueue trelloQueue)
        {
            _boardRepository = boardRepository;
            _trelloQueue = trelloQueue;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBoard([FromBody] CreateBoardRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var boardId = await _boardRepository.CreateBoardAsync(
                request.BoardName,
                userId
            );
            _trelloQueue.LocalBoardChanged(boardId);

            return Ok(new
            {
                BoardID = boardId,
                BoardName = request.BoardName.Trim(),
                CreatedByUserID = userId
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyBoards()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var boards = await _boardRepository.GetBoardsByUserIdAsync(userId);

            return Ok(
                boards.Select(b => new
                {
                    BoardID = b.BoardID,
                    BoardName = b.BoardName
                })
            );
        }

        [HttpPost("{boardId:int}")]
        public async Task<IActionResult> UpdateBoardName(int boardId, [FromBody] UpdateBoardNameRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            await _boardRepository.UpdateBoardNameAsync(
                boardId,
                request.BoardName,
                userId
            );
            _trelloQueue.LocalBoardRenamed(boardId);

            return Ok(new
            {
                BoardID = boardId,
                BoardName = request.BoardName.Trim()
            });
        }
    }
}