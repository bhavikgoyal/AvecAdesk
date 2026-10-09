using AvecADeskApi.DTOs.Card;
using AvecADeskApi.DTOs.Trello;
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
    public class CardCommentController : ControllerBase
    {
        private static readonly HashSet<string> AllowedActivityTypes = new(StringComparer.Ordinal)
        {
            "updateCheckItemStateOnCard",
            "cardChecklistAdded",
            "cardChecklistRemoved",
            "cardDueDateChanged",
            "cardDueDateRemoved",
            "cardAttachmentAdded",
            "cardAttachmentDeleted",
            "cardMoved",
            "cardMemberAdded",
            "cardMemberRemoved",
            "cardRenamed",
        };

        private readonly ICardCommentRepository _repo;
        private readonly LogHelper _logHelper;
        private readonly TrelloChangeTracker _trello;

        public CardCommentController(ICardCommentRepository repo, LogHelper logHelper, TrelloChangeTracker trello)
        {
            _repo = repo;
            _logHelper = logHelper;
            _trello = trello;
        }

        [HttpGet("card/{cardId:int}")]
        public async Task<IActionResult> GetByCard(int cardId)
        {
            if (cardId <= 0) return BadRequest(new { message = "Valid CardID is required." });

            try
            {
                var userId = GetCurrentUserId();
                var comments = await _repo.GetByCardIdAsync(cardId);
                comments.ForEach(c => c.CanEdit = userId.HasValue && c.UserID == userId.Value);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentController)}.{nameof(GetByCard)}", ex);
                return StatusCode(500, new { message = "Error loading comments", detail = ex.Message });
            }
        }

        [HttpGet("activity/{cardId:int}")]
        public async Task<IActionResult> GetActivity(int cardId)
        {
            if (cardId <= 0) return BadRequest(new { message = "Valid CardID is required." });

            try
            {
                var activity = await _repo.GetActivityByCardIdAsync(cardId);
                return Ok(activity);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentController)}.{nameof(GetActivity)}", ex);
                return StatusCode(500, new { message = "Error loading activity", detail = ex.Message });
            }
        }

        [HttpPost("activity/log")]
        public async Task<IActionResult> LogActivity([FromBody] LogCardActivityRequest request)
        {
            if (request == null || request.CardID <= 0)
                return BadRequest(new { message = "Valid CardID is required." });

            request.ActivityType = (request.ActivityType ?? string.Empty).Trim();
            if (!AllowedActivityTypes.Contains(request.ActivityType))
                return BadRequest(new { message = "Unsupported activity type." });

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Invalid token" });

            try
            {
                await _repo.LogActivityAsync(request, userId.Value);
                return Ok(new { message = "Activity logged." });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentController)}.{nameof(LogActivity)}", ex);
                return StatusCode(500, new { message = "Error logging activity", detail = ex.Message });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] SaveCardCommentRequest request)
        {
            if (request == null || request.CardID <= 0)
                return BadRequest(new { message = "Valid CardID is required." });
            if (string.IsNullOrWhiteSpace(request.CommentText))
                return BadRequest(new { message = "Comment text is required." });

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Invalid token" });

            try
            {
                var comment = await _repo.CreateAsync(request.CardID, userId.Value, request.CommentText.Trim());
                if (comment != null) comment.CanEdit = true;
                _trello.CardChanged(request.CardID);
                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentController)}.{nameof(Create)}", ex);
                return StatusCode(500, new { message = "Error adding comment", detail = ex.Message });
            }
        }

        [HttpPost("update/{commentId:int}")]
        public async Task<IActionResult> Update(int commentId, [FromBody] SaveCardCommentRequest request)
        {
            if (commentId <= 0) return BadRequest(new { message = "Valid CommentID is required." });
            if (request == null || string.IsNullOrWhiteSpace(request.CommentText))
                return BadRequest(new { message = "Comment text is required." });

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Invalid token" });

            try
            {
                var comment = await _repo.UpdateAsync(commentId, userId.Value, request.CommentText.Trim());
                if (comment == null) return NotFound(new { message = "Comment not found." });
                comment.CanEdit = true;
                _trello.CardChanged(comment.CardID);
                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentController)}.{nameof(Update)}", ex);
                return StatusCode(500, new { message = "Error updating comment", detail = ex.Message });
            }
        }

        [HttpPost("delete/{commentId:int}")]
        public async Task<IActionResult> Delete(int commentId)
        {
            if (commentId <= 0) return BadRequest(new { message = "Valid CommentID is required." });

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Invalid token" });

            try
            {
                var trelloRef = await _trello.GetRefAsync(TrelloEntityTypes.Comment, commentId);
                var deleted = await _repo.DeleteAsync(commentId, userId.Value);
                if (!deleted) return NotFound(new { message = "Comment not found or you can only delete your own comment." });
                await _trello.DeletedAsync(TrelloEntityTypes.Comment, trelloRef);
                return Ok(new { message = "Comment deleted successfully." });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentController)}.{nameof(Delete)}", ex);
                return StatusCode(500, new { message = "Error deleting comment", detail = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
