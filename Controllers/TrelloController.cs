using AvecADeskApi.DTOs.Trello;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Services.Trello;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    /// <summary>Read-only health check for the automatic Trello sync (no UI uses it).</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TrelloController : ControllerBase
    {
        private readonly TrelloClient _trello;
        private readonly ITrelloSyncRepository _repo;
        private readonly IConfiguration _configuration;

        public TrelloController(TrelloClient trello, ITrelloSyncRepository repo, IConfiguration configuration)
        {
            _trello = trello;
            _repo = repo;
            _configuration = configuration;
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
