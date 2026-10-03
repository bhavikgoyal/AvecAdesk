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
    public class CardCoverController : ControllerBase
    {
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedSizes = { "normal", "full" };
        private static readonly string[] AllowedBrightness = { "light", "dark" };
        private const long MaxImageBytes = 10_000_000;

        private readonly ICardCoverRepository _repo;
        private readonly LogHelper _logHelper;
        private readonly IWebHostEnvironment _env;

        public CardCoverController(ICardCoverRepository repo, LogHelper logHelper, IWebHostEnvironment env)
        {
            _repo = repo;
            _logHelper = logHelper;
            _env = env;
        }

        [HttpGet("colors")]
        public async Task<IActionResult> GetColors()
        {
            try
            {
                var colors = await _repo.GetColorsAsync();
                return Ok(colors);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCoverController)}.{nameof(GetColors)}", ex);
                return StatusCode(500, new { message = "Error loading cover colors", detail = ex.Message });
            }
        }

        [HttpGet("card/{cardId:int}")]
        public async Task<IActionResult> GetByCard(int cardId)
        {
            if (cardId <= 0) return BadRequest(new { message = "Valid CardID is required." });

            try
            {
                var cover = await _repo.GetByCardIdAsync(cardId);
                return Ok(cover);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCoverController)}.{nameof(GetByCard)}", ex);
                return StatusCode(500, new { message = "Error loading card cover", detail = ex.Message });
            }
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveCardCoverRequest request)
        {
            if (request == null || request.CardID <= 0)
                return BadRequest(new { message = "Valid CardID is required." });

            request.Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim().ToLowerInvariant();
            request.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
            request.Size = string.IsNullOrWhiteSpace(request.Size) ? "normal" : request.Size.Trim().ToLowerInvariant();
            request.Brightness = string.IsNullOrWhiteSpace(request.Brightness) ? "light" : request.Brightness.Trim().ToLowerInvariant();

            if (request.Color == null && request.ImageUrl == null)
                return BadRequest(new { message = "Cover color or cover image is required." });

            if (!AllowedSizes.Contains(request.Size))
                return BadRequest(new { message = "Invalid cover size. Allowed: normal, full." });

            if (!AllowedBrightness.Contains(request.Brightness))
                return BadRequest(new { message = "Invalid cover brightness. Allowed: light, dark." });

            try
            {
                var cover = await _repo.SaveAsync(request, GetCurrentUserId());
                return Ok(cover);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCoverController)}.{nameof(Save)}", ex);
                return StatusCode(500, new { message = "Error saving card cover", detail = ex.Message });
            }
        }

        [HttpPost("upload/{cardId:int}")]
        [RequestSizeLimit(MaxImageBytes)]
        public async Task<IActionResult> UploadImage(int cardId, IFormFile file, [FromForm] string? size)
        {
            if (cardId <= 0) return BadRequest(new { message = "Valid CardID is required." });
            if (file == null || file.Length == 0) return BadRequest(new { message = "No image uploaded." });
            if (file.Length > MaxImageBytes) return BadRequest(new { message = "Image must be 10 MB or smaller." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
                return BadRequest(new { message = "Unsupported image type. Allowed: JPG, PNG, GIF, WEBP." });

            var coverSize = string.IsNullOrWhiteSpace(size) ? "normal" : size.Trim().ToLowerInvariant();
            if (!AllowedSizes.Contains(coverSize)) coverSize = "normal";

            try
            {
                var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads", "card-covers");
                Directory.CreateDirectory(uploadsRoot);

                var fileName = $"{cardId}_{DateTime.UtcNow.Ticks}{extension}";
                await using (var stream = new FileStream(Path.Combine(uploadsRoot, fileName), FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var cover = await _repo.SaveAsync(new SaveCardCoverRequest
                {
                    CardID = cardId,
                    ImageUrl = $"/uploads/card-covers/{fileName}",
                    Size = coverSize,
                    Brightness = "dark",
                }, GetCurrentUserId());

                return Ok(cover);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCoverController)}.{nameof(UploadImage)}", ex);
                return StatusCode(500, new { message = "Error uploading cover image", detail = ex.Message });
            }
        }

        [HttpPost("remove/{cardId:int}")]
        public async Task<IActionResult> Remove(int cardId)
        {
            if (cardId <= 0) return BadRequest(new { message = "Valid CardID is required." });

            try
            {
                await _repo.RemoveAsync(cardId);
                return Ok(new { message = "Cover removed successfully." });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCoverController)}.{nameof(Remove)}", ex);
                return StatusCode(500, new { message = "Error removing card cover", detail = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
