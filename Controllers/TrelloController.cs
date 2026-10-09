using AvecADeskApi.DTOs.Trello;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Services.Trello;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    /// <summary>Health check for the automatic Trello sync, plus an on-demand sync for an opened card.</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TrelloController : ControllerBase
    {
        private readonly TrelloClient _trello;
        private readonly ITrelloSyncRepository _repo;
        private readonly TrelloSyncQueue _queue;
        private readonly IConfiguration _configuration;

        public TrelloController(TrelloClient trello, ITrelloSyncRepository repo, TrelloSyncQueue queue, IConfiguration configuration)
        {
            _trello = trello;
            _repo = repo;
            _queue = queue;
            _configuration = configuration;
        }

        /// <summary>Queues a sync of the card's board; the result shows up on the next checklist/comment load.</summary>
        [HttpPost("sync/card/{cardId:int}")]
        public IActionResult SyncCard(int cardId)
        {
            if (cardId <= 0) return BadRequest(new { message = "Valid CardID is required." });
            _queue.LocalCardChanged(cardId);
            return Accepted(new { queued = TrelloSyncWorker.IsAutoSyncEnabled(_configuration) && _trello.IsConfigured });
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(CancellationToken ct)
        {
            var callbackUrl = TrelloAutoSyncService.GetWebhookCallbackUrl(_configuration);
            var status = new TrelloStatusResponse
            {
                Configured = _trello.IsConfigured,
                AutoSyncEnabled = TrelloSyncWorker.IsAutoSyncEnabled(_configuration),
                WebhookEnabled = callbackUrl != null,
                WebhookCallbackUrl = callbackUrl,
            };
            if (!status.Configured) return Ok(status);

            try
            {
                var me = await _trello.GetMeAsync(ct);
                status.Connected = true;
                status.FullName = me.FullName;
                status.Username = me.Username;
                status.LinkedBoards = (await _repo.GetBoardLinksAsync()).Count;
            }
            catch (Exception ex) when (ex is TrelloApiException or HttpRequestException or TaskCanceledException)
            {
                status.Error = ex.Message;
            }
            return Ok(status);
        }
    }
}
