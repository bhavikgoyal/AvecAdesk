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
    public class CardAttachmentController : ControllerBase
    {
        private const long MaxFileBytes = 25_000_000;
        private const string UploadFolder = "card-attachments";

        // Server par execute ho sakne wali files block
        private static readonly string[] BlockedExtensions =
        {
            ".exe", ".dll", ".bat", ".cmd", ".com", ".msi", ".ps1", ".vbs", ".js", ".jse",
            ".sh", ".scr", ".pif", ".cpl", ".jar", ".hta", ".asp", ".aspx", ".cshtml", ".php", ".config",
        };

        private readonly ICardAttachmentRepository _repo;
        private readonly LogHelper _logHelper;
        private readonly IWebHostEnvironment _env;

        public CardAttachmentController(ICardAttachmentRepository repo, LogHelper logHelper, IWebHostEnvironment env)
        {
            _repo = repo;
            _logHelper = logHelper;
            _env = env;
        }

        [HttpGet("card/{cardId:int}")]
        public async Task<IActionResult> GetByCard(int cardId)
        {
            if (cardId <= 0) return BadRequest(new { message = "Valid CardID is required." });

            try
            {
                return Ok(await _repo.GetByCardIdAsync(cardId));
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentController)}.{nameof(GetByCard)}", ex);
                return StatusCode(500, new { message = "Error loading attachments", detail = ex.Message });
            }
        }

        [HttpPost("upload/{cardId:int}")]
        [RequestSizeLimit(MaxFileBytes + 1_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileBytes + 1_000_000)]
        public async Task<IActionResult> Upload(int cardId, IFormFile file)
        {
            if (cardId <= 0) return BadRequest(new { message = "Valid CardID is required." });
            if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });
            if (file.Length > MaxFileBytes) return BadRequest(new { message = "File must be 25 MB or smaller." });

            var originalName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalName).ToLowerInvariant();
            if (BlockedExtensions.Contains(extension))
                return BadRequest(new { message = $"{extension} files are not allowed." });

            string? fileUrl = null;
            try
            {
                var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads", UploadFolder);
                Directory.CreateDirectory(uploadsRoot);

                var storedName = $"{cardId}_{DateTime.UtcNow.Ticks}{extension}";
                await using (var stream = new FileStream(Path.Combine(uploadsRoot, storedName), FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                fileUrl = $"/uploads/{UploadFolder}/{storedName}";

                var created = await _repo.CreateAsync(new NewCardAttachment
                {
                    CardID = cardId,
                    IsLink = false,
                    DisplayName = originalName,
                    FileUrl = fileUrl,
                    FileName = originalName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                }, GetCurrentUserId());

                return Ok(created);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentController)}.{nameof(Upload)}", ex);
                TryDeleteStoredFile(fileUrl);
                return StatusCode(500, new { message = "Error uploading attachment", detail = ex.Message });
            }
        }

        [HttpPost("link")]
        public async Task<IActionResult> AddLink([FromBody] AddCardAttachmentLinkRequest request)
        {
            if (request == null || request.CardID <= 0)
                return BadRequest(new { message = "Valid CardID is required." });

            var url = NormalizeUrl(request.Url);
            if (url == null)
                return BadRequest(new { message = "Please enter a valid link (http or https)." });

            try
            {
                var created = await _repo.CreateAsync(new NewCardAttachment
                {
                    CardID = request.CardID,
                    IsLink = true,
                    DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? url : request.DisplayName.Trim(),
                    FileUrl = url,
                }, GetCurrentUserId());

                return Ok(created);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentController)}.{nameof(AddLink)}", ex);
                return StatusCode(500, new { message = "Error adding link", detail = ex.Message });
            }
        }

        [HttpPost("update/{attachmentId:int}")]
        public async Task<IActionResult> Update(int attachmentId, [FromBody] UpdateCardAttachmentRequest request)
        {
            if (attachmentId <= 0 || request == null)
                return BadRequest(new { message = "Valid AttachmentID is required." });

            if (!string.IsNullOrWhiteSpace(request.LinkUrl))
            {
                var url = NormalizeUrl(request.LinkUrl);
                if (url == null) return BadRequest(new { message = "Please enter a valid link (http or https)." });
                request.LinkUrl = url;
            }

            try
            {
                var updated = await _repo.UpdateAsync(attachmentId, request);
                return updated == null ? NotFound(new { message = "Attachment not found." }) : Ok(updated);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentController)}.{nameof(Update)}", ex);
                return StatusCode(500, new { message = "Error updating attachment", detail = ex.Message });
            }
        }

        [HttpPost("delete/{attachmentId:int}")]
        public async Task<IActionResult> Delete(int attachmentId)
        {
            if (attachmentId <= 0) return BadRequest(new { message = "Valid AttachmentID is required." });

            try
            {
                var result = await _repo.DeleteAsync(attachmentId);
                if (!result.Deleted) return NotFound(new { message = "Attachment not found." });

                if (!result.IsLink) TryDeleteStoredFile(result.FileUrl);

                return Ok(new { message = "Attachment deleted successfully." });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentController)}.{nameof(Delete)}", ex);
                return StatusCode(500, new { message = "Error deleting attachment", detail = ex.Message });
            }
        }

        private void TryDeleteStoredFile(string? fileUrl)
        {
            var prefix = $"/uploads/{UploadFolder}/";
            if (string.IsNullOrWhiteSpace(fileUrl) || !fileUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var fileName = Path.GetFileName(fileUrl);
                var fullPath = Path.Combine(_env.ContentRootPath, "uploads", UploadFolder, fileName);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentController)}.{nameof(TryDeleteStoredFile)}", ex);
            }
        }

        private static string? NormalizeUrl(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var value = raw.Trim();
            if (!value.Contains("://")) value = $"https://{value}";

            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri.ToString()
                : null;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
