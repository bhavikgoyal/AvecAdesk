using AvecADeskApi.DTOs.Card;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CardMemberController : ControllerBase
    {
        private readonly ICardMemberRepository _repo;
        private readonly LogHelper _logHelper;

        public CardMemberController(ICardMemberRepository repo, LogHelper logHelper)
        {
            _repo = repo;
            _logHelper = logHelper;
        }

        [HttpGet("{cardId:int}")]
        public async Task<IActionResult> GetMembers(int cardId)
        {
            try
            {
                var members = await _repo.GetCardMembersAsync(cardId);
                return Ok(members);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardMemberController)}.{nameof(GetMembers)}", ex);
                return StatusCode(500, new { message = "Error fetching card members" });
            }
        }

        
        [HttpPost("{cardId:int}/add")]
        public async Task<IActionResult> AddMember(int cardId, [FromBody] AddCardMemberRequest request)
        {
            try
            {
                await _repo.AddCardMemberAsync(cardId, request.UserID);
                return Ok(new { message = "Member added successfully" });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardMemberController)}.{nameof(AddMember)}", ex);
                return StatusCode(500, new { message = "Error adding member" });
            }
        }

       
        [HttpDelete("{cardId:int}/remove/{userId:int}")]
        public async Task<IActionResult> RemoveMember(int cardId, int userId)
        {
            try
            {
                await _repo.RemoveCardMemberAsync(cardId, userId);
                return Ok(new { message = "Member removed successfully" });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardMemberController)}.{nameof(RemoveMember)}", ex);
                return StatusCode(500, new { message = "Error removing member" });
            }
        }
    }
}