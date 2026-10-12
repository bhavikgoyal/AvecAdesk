using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AvecADeskApi.Services.Trello;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class TrelloWebhookController : ControllerBase
    {
        private static readonly HashSet<string> BoardListActions = new(StringComparer.OrdinalIgnoreCase)
        {
            "createBoard", "copyBoard", "addMemberToBoard", "addToOrganizationBoard",
        };

        private readonly TrelloSyncQueue _queue;
        private readonly IConfiguration _configuration;

        public TrelloWebhookController(TrelloSyncQueue queue, IConfiguration configuration)
        {
            _queue = queue;
            _configuration = configuration;
        }

        [HttpHead]
        [HttpGet]
        public IActionResult Verify() => Ok();

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                body = await reader.ReadToEndAsync();

            if (!IsSignatureValid(body))
                return Unauthorized();

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var actionType = GetString(root, "action", "type");
                var boardId = GetString(root, "action", "data", "board", "id");

                if (actionType != null && BoardListActions.Contains(actionType))
                    _queue.Discover();
                else if (boardId != null)
                    _queue.TrelloBoardChanged(boardId);
            }
            catch (JsonException)
            {
            }

            return Ok();
        }

        private bool IsSignatureValid(string body)
        {
            var secret = _configuration["Trello:ApiSecret"];
            if (string.IsNullOrWhiteSpace(secret)) return true;

            var callbackUrl = TrelloAutoSyncService.GetWebhookCallbackUrl(_configuration);
            var header = Request.Headers["X-Trello-Webhook"].ToString();
            if (callbackUrl == null || string.IsNullOrEmpty(header)) return false;

            var headerBytes = Encoding.UTF8.GetBytes(header);
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret));

            foreach (var encoding in new[] { Encoding.UTF8, Encoding.Latin1 })
            {
                var expected = Convert.ToBase64String(hmac.ComputeHash(encoding.GetBytes(body + callbackUrl)));
                if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), headerBytes))
                    return true;
            }
            return false;
        }

        private static string? GetString(JsonElement element, params string[] path)
        {
            foreach (var name in path)
            {
                if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out element))
                    return null;
            }
            return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        }
    }
}
