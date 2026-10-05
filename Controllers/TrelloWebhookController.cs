using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AvecADeskApi.Services.Trello;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    /// <summary>
    /// Callback URL for Trello webhooks (Trello:WebhookCallbackUrl must point here).
    /// The payload is only used as a hint for which board changed - the worker re-reads
    /// everything from the Trello API - so a forged call can at most trigger an extra sync.
    /// </summary>
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

        /// <summary>Trello sends HEAD when the webhook is created to check the URL is reachable.</summary>
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

        /// <summary>
        /// Checked only when Trello:ApiSecret is set: X-Trello-Webhook must be
        /// base64(HMAC-SHA1(secret, body + callbackURL)).
        /// </summary>
        private bool IsSignatureValid(string body)
        {
            var secret = _configuration["Trello:ApiSecret"];
            if (string.IsNullOrWhiteSpace(secret)) return true;

            var callbackUrl = TrelloAutoSyncService.GetWebhookCallbackUrl(_configuration);
            var header = Request.Headers["X-Trello-Webhook"].ToString();
            if (callbackUrl == null || string.IsNullOrEmpty(header)) return false;

            var headerBytes = Encoding.UTF8.GetBytes(header);
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret));

            // Trello hashes non-ASCII text as single bytes (Latin-1), so accept either encoding.
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
