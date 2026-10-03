using AvecADeskApi.DTOs.List;
using AvecADeskApi.Interfaces;
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

        public ListController(IListRepository listRepository)
        {
            _listRepository = listRepository;
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