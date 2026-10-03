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
    }
}