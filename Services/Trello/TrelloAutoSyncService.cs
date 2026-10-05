using System.Collections.Concurrent;
using AvecADeskApi.DTOs.Trello;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;

namespace AvecADeskApi.Services.Trello;

/// <summary>
/// Keeps every AiDesk board paired with a Trello board without any manual step:
/// boards that exist on only one side are linked (by imported list IDs, then by name)
/// or created on the other side, and each pair is synced with <see cref="TrelloSyncService"/>.
/// Only <see cref="TrelloSyncWorker"/> calls this, one job at a time, so two runs never
/// race to create the same board twice.
/// </summary>
public class TrelloAutoSyncService
{
    private static readonly TimeSpan FailedCreateRetryAfter = TimeSpan.FromHours(1);

    // Boards whose create failed (e.g. Trello free plan board limit); retried after FailedCreateRetryAfter.
    private static readonly ConcurrentDictionary<string, DateTime> CreateBackoff = new();
    private static readonly ConcurrentDictionary<string, byte> RegisteredWebhooks = new();
    private static string? _memberId;
    private static bool _ownerWarningLogged;

    private readonly TrelloClient _trello;
    private readonly ITrelloSyncRepository _repo;
    private readonly IBoardRepository _boards;
    private readonly TrelloSyncService _sync;
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;
    private readonly ILogger<TrelloAutoSyncService> _logger;

    public TrelloAutoSyncService(
        TrelloClient trello,
        ITrelloSyncRepository repo,
        IBoardRepository boards,
        TrelloSyncService sync,
        IConfiguration configuration,
        LogHelper logHelper,
        ILogger<TrelloAutoSyncService> logger)
    {
        _trello = trello;
        _repo = repo;
        _boards = boards;
        _sync = sync;
        _configuration = configuration;
        _logHelper = logHelper;
        _logger = logger;
    }

    public static string? GetWebhookCallbackUrl(IConfiguration configuration)
    {
        var url = configuration["Trello:WebhookCallbackUrl"]?.Trim();
        return string.IsNullOrWhiteSpace(url) ? null : url;
    }

    private string? WebhookCallbackUrl => GetWebhookCallbackUrl(_configuration);

    /* ------------------------------- entry points ------------------------------- */

    public async Task ProcessAsync(TrelloSyncJob job, CancellationToken ct)
    {
        switch (job.Kind)
        {
            case TrelloJobKind.Discover:
                await DiscoverAsync(ct);
                break;

            case TrelloJobKind.LocalBoardChanged:
                await SyncLocalBoardAsync(job.LocalId, ct);
                break;

            case TrelloJobKind.LocalCardChanged:
                var boardId = await _repo.GetCardBoardIdAsync(job.LocalId);
                if (boardId.HasValue) await SyncLocalBoardAsync(boardId.Value, ct);
                break;

            case TrelloJobKind.LocalBoardRenamed:
                await RenameOnTrelloAsync(job.LocalId, ct);
                break;

            case TrelloJobKind.TrelloBoardChanged:
                var link = (await _repo.GetBoardLinksAsync()).FirstOrDefault(l => l.TrelloBoardID == job.TrelloId);
                if (link == null) await DiscoverAsync(ct);
                else await SyncLinkAsync(link, ct);
                break;
        }
    }

    /// <summary>Periodic safety net: picks up anything a webhook or controller hook missed.</summary>
    public async Task FullRunAsync(CancellationToken ct)
    {
        var syncedByDiscover = await DiscoverAsync(ct);

        foreach (var link in await _repo.GetBoardLinksAsync())
        {
            ct.ThrowIfCancellationRequested();
            // LocalBoardName is null when the AiDesk board was deleted after linking.
            if (link.LocalBoardName != null && !syncedByDiscover.Contains(link.LocalBoardID))
                await SyncLinkAsync(link, ct);
        }

        await EnsureWebhooksAsync(ct);
    }

    /* ---------------------------------- boards ---------------------------------- */

    /// <summary>Returns the AiDesk board IDs that were linked (and synced) in this call.</summary>
    private async Task<HashSet<int>> DiscoverAsync(CancellationToken ct)
    {
        var newLinks = new HashSet<int>();

        var ownerId = await ResolveOwnerUserIdAsync();
        if (ownerId <= 0)
        {
            if (!_ownerWarningLogged)
            {
                _ownerWarningLogged = true;
                _logHelper.LogError(nameof(TrelloAutoSyncService),
                    new InvalidOperationException("Trello auto-sync is paused: no active user found for Trello:DefaultBoardOwnerRoleId."));
            }
            return newLinks;
        }

        await _repo.CleanupOrphanLinksAsync();

        var trelloBoards = (await _trello.GetBoardsAsync(ct)).Where(b => !b.Closed).ToList();
        var localBoards = await _repo.GetLocalBoardsAsync();

        // Every stored link counts, so a Trello board that is linked can never be paired or copied again.
        var linkedTrelloIds = (await _repo.GetBoardLinksAsync()).Select(l => l.TrelloBoardID).ToHashSet();
        var unlinkedTrello = trelloBoards.Where(t => !linkedTrelloIds.Contains(t.Id)).ToList();
        var unlinkedLocal = localBoards.Where(b => b.TrelloBoardID == null).ToList();

        // 1) Boards imported from Trello earlier: their lists still carry Trello list IDs.
        if (unlinkedTrello.Count > 0 && unlinkedLocal.Count > 0)
        {
            var boardByTrelloList = (await _repo.GetBoardListMapsAsync())
                .GroupBy(m => m.TrelloListID)
                .ToDictionary(g => g.Key, g => g.First().LocalBoardID);

            if (boardByTrelloList.Count > 0)
            {
                foreach (var tb in unlinkedTrello.ToList())
                {
                    var trelloLists = await _trello.GetListsAsync(tb.Id, ct);
                    var localId = trelloLists
                        .Select(l => boardByTrelloList.TryGetValue(l.Id, out var b) ? b : 0)
                        .Where(b => b > 0 && unlinkedLocal.Any(u => u.BoardID == b))
                        .GroupBy(b => b)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .FirstOrDefault();

                    if (localId > 0 && await TryLinkAsync(localId, tb, "trello", ownerId))
                    {
                        newLinks.Add(localId);
                        unlinkedTrello.Remove(tb);
                        unlinkedLocal.RemoveAll(b => b.BoardID == localId);
                    }
                }
            }
        }

        // 2) Same name on both sides.
        foreach (var tb in unlinkedTrello.ToList())
        {
            var match = unlinkedLocal.FirstOrDefault(b => SameText(b.BoardName, tb.Name));
            if (match != null && await TryLinkAsync(match.BoardID, tb, "trello", ownerId))
            {
                newLinks.Add(match.BoardID);
                unlinkedTrello.Remove(tb);
                unlinkedLocal.Remove(match);
            }
        }

        // 3) Trello-only boards -> new AiDesk board (owned by the configured Super Admin).
        foreach (var tb in unlinkedTrello)
        {
            var key = $"aidesk:{tb.Id}";
            if (InBackoff(key)) continue;
            try
            {
                var localId = await _boards.CreateBoardAsync(tb.Name, ownerId);
                if (await TryLinkAsync(localId, tb, "trello", ownerId))
                {
                    newLinks.Add(localId);
                    _logger.LogInformation("Trello board \"{Name}\" created in AiDesk as board {BoardId}", tb.Name, localId);
                }
                else
                {
                    // Without this the unlinked copy would be pushed back to Trello on the next run.
                    CreateBackoff[key] = DateTime.UtcNow;
                    CreateBackoff[$"trello:{localId}"] = DateTime.UtcNow;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                CreateBackoff[key] = DateTime.UtcNow;
                _logHelper.LogError($"{nameof(TrelloAutoSyncService)}.CreateAiDeskBoard \"{tb.Name}\"", ex);
            }
        }

        // 4) AiDesk-only boards -> new Trello board.
        foreach (var lb in unlinkedLocal)
        {
            if (string.IsNullOrWhiteSpace(lb.BoardName)) continue;
            var key = $"trello:{lb.BoardID}";
            if (InBackoff(key)) continue;
            try
            {
                var tb = await _trello.CreateBoardAsync(lb.BoardName.Trim(), ct);
                if (await TryLinkAsync(lb.BoardID, tb, "aidesk", ownerId))
                {
                    newLinks.Add(lb.BoardID);
                    _logger.LogInformation("AiDesk board {BoardId} \"{Name}\" created in Trello", lb.BoardID, lb.BoardName);
                }
                else
                {
                    CreateBackoff[key] = DateTime.UtcNow;
                    CreateBackoff[$"aidesk:{tb.Id}"] = DateTime.UtcNow;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                CreateBackoff[key] = DateTime.UtcNow;
                _logHelper.LogError($"{nameof(TrelloAutoSyncService)}.CreateTrelloBoard \"{lb.BoardName}\"", ex);
            }
        }

        foreach (var localId in newLinks)
        {
            var link = await _repo.GetBoardLinkAsync(localId);
            if (link != null) await SyncLinkAsync(link, ct);
        }

        if (newLinks.Count > 0)
            await EnsureWebhooksAsync(ct);

        return newLinks;
    }

    private async Task<bool> TryLinkAsync(int localBoardId, TrelloBoard trelloBoard, string winner, int userId)
    {
        try
        {
            await _repo.SaveBoardLinkAsync(localBoardId, trelloBoard.Id, trelloBoard.Name, winner, userId);
            await _repo.SetStatusAsync(localBoardId, "Linked automatically", activate: true);
            return true;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(TrelloAutoSyncService)}.Link board {localBoardId} -> {trelloBoard.Id}", ex);
            return false;
        }
    }

    private async Task SyncLocalBoardAsync(int localBoardId, CancellationToken ct)
    {
        var link = await _repo.GetBoardLinkAsync(localBoardId);
        if (link == null) await DiscoverAsync(ct);
        else await SyncLinkAsync(link, ct);
    }

    private async Task SyncLinkAsync(TrelloBoardLinkResponse link, CancellationToken ct)
    {
        try
        {
            var result = await _sync.SyncBoardAsync(link.LocalBoardID, link.CreatedBy, dryRun: false, ct: ct);
            _logger.LogInformation("Trello sync board {BoardId}: {Summary}", link.LocalBoardID, result.Summary);
        }
        catch (TrelloSyncBusyException)
        {
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logHelper.LogError($"{nameof(TrelloAutoSyncService)}.Sync board {link.LocalBoardID}", ex);
        }
    }

    private async Task RenameOnTrelloAsync(int localBoardId, CancellationToken ct)
    {
        var link = await _repo.GetBoardLinkAsync(localBoardId);
        if (link == null)
        {
            await DiscoverAsync(ct);
            return;
        }

        var name = (await _repo.GetLocalBoardsAsync()).FirstOrDefault(b => b.BoardID == localBoardId)?.BoardName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name == link.TrelloBoardName) return;

        try
        {
            await _trello.RenameBoardAsync(link.TrelloBoardID, name, ct);
            await _repo.SaveBoardLinkAsync(localBoardId, link.TrelloBoardID, name, link.InitialWinner, link.CreatedBy);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logHelper.LogError($"{nameof(TrelloAutoSyncService)}.Rename board {localBoardId}", ex);
        }
    }

    /* --------------------------------- webhooks --------------------------------- */

    /// <summary>
    /// One webhook on the Trello member (fires on createBoard) and one per linked board
    /// (cards/lists). Webhooks for an old callback URL (e.g. a previous ngrok URL) are removed.
    /// </summary>
    public async Task EnsureWebhooksAsync(CancellationToken ct)
    {
        var url = WebhookCallbackUrl;
        if (url == null) return;

        try
        {
            var stored = await _repo.GetWebhooksAsync();

            foreach (var stale in stored.Where(s => s.CallbackUrl != url))
            {
                await _trello.DeleteWebhookAsync(stale.WebhookID, ct);
                await _repo.DeleteWebhookAsync(stale.WebhookID);
            }

            _memberId ??= (await _trello.GetMeAsync(ct)).Id;

            var models = new List<(string Id, string Description)> { (_memberId, "AiDesk: new Trello boards") };
            models.AddRange((await _repo.GetBoardLinksAsync())
                .Select(l => (l.TrelloBoardID, $"AiDesk board {l.LocalBoardID}")));

            foreach (var (idModel, description) in models)
            {
                var key = $"{url}|{idModel}";
                if (RegisteredWebhooks.ContainsKey(key)) continue;

                if (stored.Any(s => s.IdModel == idModel && s.CallbackUrl == url))
                {
                    RegisteredWebhooks[key] = 0;
                    continue;
                }

                try
                {
                    var created = await _trello.CreateWebhookAsync(url, idModel, description, ct);
                    if (created != null) await _repo.SaveWebhookAsync(created.Id, idModel, url);
                    RegisteredWebhooks[key] = 0;
                }
                catch (TrelloApiException ex)
                {
                    // Usually the callback URL is not reachable yet (API/ngrok not running); retried next run.
                    _logHelper.LogError($"{nameof(TrelloAutoSyncService)}.CreateWebhook {idModel}", ex);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logHelper.LogError($"{nameof(TrelloAutoSyncService)}.{nameof(EnsureWebhooksAsync)}", ex);
        }
    }

    /* --------------------------------- helpers ---------------------------------- */

    /// <summary>First active user with Trello:DefaultBoardOwnerRoleId (1 = Super Admin).</summary>
    private async Task<int> ResolveOwnerUserIdAsync()
    {
        var roleId = _configuration.GetValue("Trello:DefaultBoardOwnerRoleId", 1);
        var owner = await _repo.GetOwnerUserIdByRoleAsync(roleId);
        if (owner > 0) return owner.Value;

        // No user with that role: fall back to whoever created the first existing link.
        var first = (await _repo.GetBoardLinksAsync()).OrderBy(l => l.CreatedAt).FirstOrDefault();
        return first?.CreatedBy ?? 0;
    }

    private static bool InBackoff(string key) =>
        CreateBackoff.TryGetValue(key, out var failedAt) && DateTime.UtcNow - failedAt < FailedCreateRetryAfter;

    private static bool SameText(string? a, string? b) =>
        string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
}
