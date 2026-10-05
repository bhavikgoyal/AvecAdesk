using AvecADeskApi.DTOs.List;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Services.Trello;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ListController : ControllerBase
    {
        private readonly IListRepository _listRepository;
        private readonly TrelloSyncQueue _trelloQueue;

        public ListController(IListRepository listRepository, TrelloSyncQueue trelloQueue)
        {
            _listRepository = listRepository;
            _trelloQueue = trelloQueue;
        }

        [HttpPost]
        public async Task<IActionResult> CreateList([FromBody] CreateListRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var listId = await _listRepository.CreateListAsync(
                request.BoardID,
                request.ListName
            );
            _trelloQueue.LocalBoardChanged(request.BoardID);

            return Ok(new
            {
                ListID = listId,
                BoardID = request.BoardID,
                ListName = request.ListName.Trim()
            });
        }

        [HttpGet("{boardId:int}")]
        public async Task<IActionResult> GetListsByBoardId(int boardId)
        {
            if (boardId <= 0)
                return BadRequest("Invalid BoardID.");

            var lists = await _listRepository.GetListsByBoardIdAsync(boardId);

            return Ok(
                lists.Select(list => new
                {
                    ListID = list.ListID,
                    BoardID = list.BoardID,
                    ListName = list.ListName,
                    Position = list.Position
                })
            );
        }
    }
}