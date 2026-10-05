using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AvecADeskApi.DTOs.Card;
using AvecADeskApi.DTOs.Trello;
using AvecADeskApi.Interfaces;

namespace AvecADeskApi.Services.Trello;

public class TrelloSyncBusyException : Exception
{
    public TrelloSyncBusyException() : base("A sync for this board is already running. Try again in a moment.") { }
}

/// <summary>
/// Two-way sync of one AiDesk board with one Trello board.
/// Synced fields: list structure, card title, description, due date and list (move).
/// Each card pair stores a hash of the last synced state, so a later run can tell
/// which side changed; if both changed, Trello wins. Deletes/archives are not
/// propagated - a card missing on one side is skipped, never recreated.
/// </summary>
public class TrelloSyncService
{
    private const int MaxDetails = 100;
    private const int MoveToEndPosition = 999999;

    private static readonly ConcurrentDictionary<int, SemaphoreSlim> BoardLocks = new();

    private readonly TrelloClient _trello;
    private readonly ITrelloSyncRepository _repo;
    private readonly IListRepository _lists;
    private readonly ICardRepository _cards;
    private readonly TimeZoneInfo _timeZone;

    public TrelloSyncService(
        TrelloClient trello,
        ITrelloSyncRepository repo,
        IListRepository lists,
        ICardRepository cards,
        IConfiguration configuration)
    {
        _trello = trello;
        _repo = repo;
        _lists = lists;
        _cards = cards;
        _timeZone = ResolveTimeZone(configuration["Trello:TimeZoneId"]);
    }

    public static string NormalizeWinner(string? value) =>
        string.Equals(value?.Trim(), "aidesk", StringComparison.OrdinalIgnoreCase) ? "aidesk" : "trello";

    public async Task<TrelloSyncResult> SyncBoardAsync(int localBoardId, int userId, bool dryRun, string? initialWinner = null, CancellationToken ct = default)
    {
        var gate = BoardLocks.GetOrAdd(localBoardId, _ => new SemaphoreSlim(1, 1));
        if (!await gate.WaitAsync(0, ct))
            throw new TrelloSyncBusyException();

        try
        {
            var link = await _repo.GetBoardLinkAsync(localBoardId)
                ?? throw new InvalidOperationException("This board is not linked to Trello.");

            var run = new SyncRun(this, link, userId, dryRun, NormalizeWinner(initialWinner ?? link.InitialWinner), ct);
            var result = await run.ExecuteAsync();

            if (!dryRun)
                await _repo.SetStatusAsync(localBoardId, result.Summary, activate: true);

            return result;
        }
        catch (Exception ex) when (!dryRun && ex is not TrelloSyncBusyException && ex is not InvalidOperationException)
        {
            await _repo.SetStatusAsync(localBoardId, $"Failed: {ex.Message}", activate: false);
            throw;
        }
        finally
        {
            gate.Release();
        }
    }

    private static TimeZoneInfo ResolveTimeZone(string? configured)
    {
        foreach (var id in new[] { configured, "India Standard Time", "Asia/Kolkata" })
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Local;
    }

    private static bool SameText(string? a, string? b) =>
        string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeText(string? value) =>
        (value ?? string.Empty).Replace("\r\n", "\n").Trim();

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed record CardState(string Title, string Desc, string Due, string? ListTrelloId)
    {
        public string Hash
        {
            get
            {
                var raw = string.Join('\u001f', Title, Desc, Due, ListTrelloId ?? string.Empty);
                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
            }
        }
    }

    /// <summary>State for a single sync run of one board.</summary>
    private sealed class SyncRun
    {
        private readonly TrelloSyncService _svc;
        private readonly TrelloBoardLinkResponse _link;
        private readonly int _userId;
        private readonly bool _dryRun;
        private readonly string _winner;
        private readonly CancellationToken _ct;
        private readonly TrelloSyncResult _result;

        private readonly Dictionary<int, string> _listLocalToTrello = new();
        private readonly Dictionary<string, int> _listTrelloToLocal = new();
        private readonly Dictionary<int, string> _localListNames = new();
        private readonly Dictionary<string, string> _trelloListNames = new();

        private int _fakeLocalId = -1;
        private int _fakeTrelloId = 1;

        public SyncRun(TrelloSyncService svc, TrelloBoardLinkResponse link, int userId, bool dryRun, string winner, CancellationToken ct)
        {
            _svc = svc;
            _link = link;
            _userId = userId;
            _dryRun = dryRun;
            _winner = winner;
            _ct = ct;
            _result = new TrelloSyncResult { DryRun = dryRun };
        }

        private int BoardId => _link.LocalBoardID;

        public async Task<TrelloSyncResult> ExecuteAsync()
        {
            var maps = await _svc._repo.GetMapsForBoardAsync(BoardId);
            await SyncListsAsync(maps.Where(m => m.EntityType == "list").ToList());
            await SyncCardsAsync(maps.Where(m => m.EntityType == "card").ToList());
            _result.Summary = BuildSummary();
            return _result;
        }

        /* -------------------------------- lists -------------------------------- */

        private async Task SyncListsAsync(List<TrelloMapRow> listMaps)
        {
            var trelloLists = (await _svc._trello.GetListsAsync(_link.TrelloBoardID, _ct))
                .Where(l => !l.Closed)
                .OrderBy(l => l.Pos)
                .ToList();
            var localLists = (await _svc._lists.GetListsByBoardIdAsync(BoardId))
                .OrderBy(l => l.Position)
                .ToList();

            foreach (var l in localLists) _localListNames[l.ListID] = l.ListName;
            foreach (var l in trelloLists) _trelloListNames[l.Id] = l.Name;

            foreach (var m in listMaps)
            {
                if (_localListNames.ContainsKey(m.LocalID)
                    && _trelloListNames.ContainsKey(m.TrelloID)
                    && !_listLocalToTrello.ContainsKey(m.LocalID)
                    && !_listTrelloToLocal.ContainsKey(m.TrelloID))
                {
                    LinkList(m.LocalID, m.TrelloID);
                }
            }

            foreach (var tl in trelloLists.Where(l => !_listTrelloToLocal.ContainsKey(l.Id)))
            {
                var match = localLists
                    .Where(l => !_listLocalToTrello.ContainsKey(l.ListID) && SameText(l.ListName, tl.Name))
                    .Select(l => (int?)l.ListID)
                    .FirstOrDefault();

                if (match.HasValue)
                {
                    if (!_dryRun) await _svc._repo.SaveMapAsync("list", match.Value, tl.Id, BoardId, null);
                    LinkList(match.Value, tl.Id);
                    _result.ListsLinked++;
                    continue;
                }

                var localId = _dryRun ? _fakeLocalId-- : await _svc._lists.CreateListAsync(BoardId, tl.Name);
                if (!_dryRun) await _svc._repo.SaveMapAsync("list", localId, tl.Id, BoardId, null);
                _localListNames[localId] = tl.Name;
                LinkList(localId, tl.Id);
                _result.ListsCreatedInAiDesk++;
                Detail($"List \"{tl.Name}\": {(_dryRun ? "will be created" : "created")} in AiDesk");
            }

            foreach (var ll in localLists.Where(l => !_listLocalToTrello.ContainsKey(l.ListID)))
            {
                var trelloId = _dryRun
                    ? $"new-list-{_fakeTrelloId++}"
                    : (await _svc._trello.CreateListAsync(_link.TrelloBoardID, ll.ListName, _ct)).Id;
                if (!_dryRun) await _svc._repo.SaveMapAsync("list", ll.ListID, trelloId, BoardId, null);
                _trelloListNames[trelloId] = ll.ListName;
                LinkList(ll.ListID, trelloId);
                _result.ListsCreatedInTrello++;
                Detail($"List \"{ll.ListName}\": {(_dryRun ? "will be created" : "created")} in Trello");
            }
        }

        private void LinkList(int localId, string trelloId)
        {
            _listLocalToTrello[localId] = trelloId;
            _listTrelloToLocal[trelloId] = localId;
        }

        /* -------------------------------- cards -------------------------------- */

        private async Task SyncCardsAsync(List<TrelloMapRow> cardMaps)
        {
            var trelloCards = (await _svc._trello.GetCardsAsync(_link.TrelloBoardID, _ct))
                .Where(c => !c.Closed)
                .OrderBy(c => c.Pos)
                .ToList();
            var localCards = (await _svc._cards.GetCardsByBoardIdAsync(BoardId, null, null, null, null))
                .Where(c => c.IsArchived != true)
                .GroupBy(c => c.CardID)
                .Select(g => g.First())
                .ToList();

            var trelloById = trelloCards.ToDictionary(c => c.Id);
            var localById = localCards.ToDictionary(c => c.CardID);
            var mappedLocalIds = cardMaps.Select(m => m.LocalID).ToHashSet();
            var mappedTrelloIds = cardMaps.Select(m => m.TrelloID).ToHashSet();

            foreach (var m in cardMaps)
            {
                var hasLocal = localById.TryGetValue(m.LocalID, out var lc);
                var hasTrello = trelloById.TryGetValue(m.TrelloID, out var tc);
                if (hasLocal && hasTrello)
                    await ReconcileAsync(lc!, tc!, m.SyncHash);
                else if (hasLocal || hasTrello)
                    _result.CardsSkipped++;
            }

            var unmappedLocal = localCards.Where(c => !mappedLocalIds.Contains(c.CardID)).ToList();

            foreach (var tc in trelloCards.Where(c => !mappedTrelloIds.Contains(c.Id)))
            {
                if (!_listTrelloToLocal.TryGetValue(tc.IdList, out var localListId))
                {
                    _result.CardsSkipped++;
                    continue;
                }

                // Pair with an existing AiDesk card of the same title (e.g. from the old Trello import)
                var candidates = unmappedLocal.Where(lc => SameText(lc.CardTitle, tc.Name)).ToList();
                var match = candidates.FirstOrDefault(lc => lc.ListID == localListId)
                            ?? (candidates.Count == 1 ? candidates[0] : null);
                if (match != null)
                {
                    unmappedLocal.Remove(match);
                    _result.CardsLinked++;
                    await ReconcileAsync(match, tc, null);
                    continue;
                }

                if (!_dryRun)
                {
                    var newCardId = await _svc._cards.CreateCardAsync(new CreateCardRequest
                    {
                        BoardID = BoardId,
                        ListID = localListId,
                        CardTitle = tc.Name.Trim(),
                        Description = NullIfBlank(tc.Desc),
                        DueDate = ToLocal(tc.Due),
                    }, _userId);
                    await _svc._repo.SaveMapAsync("card", newCardId, tc.Id, BoardId, TrelloState(tc).Hash);
                }
                _result.CardsCreatedInAiDesk++;
                Detail($"Card \"{tc.Name}\": {(_dryRun ? "will be created" : "created")} in AiDesk");
            }

            foreach (var lc in unmappedLocal)
            {
                if (string.IsNullOrWhiteSpace(lc.CardTitle)
                    || lc.ListID is not int localListId
                    || !_listLocalToTrello.TryGetValue(localListId, out var trelloListId))
                {
                    _result.CardsSkipped++;
                    continue;
                }

                if (!_dryRun)
                {
                    var state = LocalState(lc);
                    var created = await _svc._trello.CreateCardAsync(
                        trelloListId, lc.CardTitle.Trim(), lc.Description, ToUtc(lc.DueDate), _ct);
                    await _svc._repo.SaveMapAsync("card", lc.CardID, created.Id, BoardId, state.Hash);
                }
                _result.CardsCreatedInTrello++;
                Detail($"Card \"{lc.CardTitle}\": {(_dryRun ? "will be created" : "created")} in Trello");
            }
        }

        private async Task ReconcileAsync(CardResponse lc, TrelloCard tc, string? syncHash)
        {
            var local = LocalState(lc);
            var trello = TrelloState(tc);

            if (local.Hash == trello.Hash)
            {
                if (!_dryRun && syncHash != local.Hash)
                    await _svc._repo.SaveMapAsync("card", lc.CardID, tc.Id, BoardId, local.Hash);
                _result.CardsUnchanged++;
                return;
            }

            // First pairing has no history, so the board's chosen side wins; afterwards Trello wins conflicts.
            var pullFromTrello = syncHash == null
                ? _winner == "trello"
                : trello.Hash != syncHash;

            if (pullFromTrello)
            {
                if (!_dryRun)
                {
                    await PullAsync(lc, tc, local, trello);
                    await _svc._repo.SaveMapAsync("card", lc.CardID, tc.Id, BoardId, trello.Hash);
                }
                _result.CardsUpdatedInAiDesk++;
                Detail($"Card \"{tc.Name}\": {(_dryRun ? "will be updated" : "updated")} in AiDesk ({DescribeChanges(local, trello)})");
            }
            else
            {
                if (!_dryRun)
                {
                    await PushAsync(lc, tc, local, trello);
                    await _svc._repo.SaveMapAsync("card", lc.CardID, tc.Id, BoardId, local.Hash);
                }
                _result.CardsUpdatedInTrello++;
                Detail($"Card \"{lc.CardTitle}\": {(_dryRun ? "will be updated" : "updated")} in Trello ({DescribeChanges(trello, local)})");
            }
        }

        private async Task PullAsync(CardResponse lc, TrelloCard tc, CardState local, CardState trello)
        {
            if (local.Title != trello.Title || local.Desc != trello.Desc || local.Due != trello.Due)
            {
                await _svc._cards.UpdateCardAsync(new UpdateCardRequest
                {
                    CardID = lc.CardID,
                    CardTitle = tc.Name.Trim(),
                    Description = NullIfBlank(tc.Desc),
                    Color = lc.Color,
                    DueDate = ToLocal(tc.Due),
                    StartDate = lc.StartDate,
                    RecurringRule = lc.RecurringRule,
                    ReminderOffsetMinutes = lc.ReminderOffsetMinutes,
                    AssignedUserID = lc.AssignedUserID,
                    CardStatusID = lc.CardStatusID,
                    CPID = lc.CPID,
                });
            }

            if (local.ListTrelloId != trello.ListTrelloId
                && _listTrelloToLocal.TryGetValue(tc.IdList, out var targetListId)
                && targetListId > 0)
            {
                await _svc._cards.MoveCardToListAsync(new MoveCardToListRequest
                {
                    CardID = lc.CardID,
                    ListID = targetListId,
                    Position = MoveToEndPosition,
                });
            }
        }

        private async Task PushAsync(CardResponse lc, TrelloCard tc, CardState local, CardState trello)
        {
            var fields = new Dictionary<string, object?>();
            if (local.Title != trello.Title && !string.IsNullOrWhiteSpace(lc.CardTitle))
                fields["name"] = lc.CardTitle.Trim();
            if (local.Desc != trello.Desc)
                fields["desc"] = lc.Description ?? string.Empty;
            if (local.Due != trello.Due)
            {
                var dueUtc = ToUtc(lc.DueDate);
                fields["due"] = dueUtc.HasValue ? DateTime.SpecifyKind(dueUtc.Value, DateTimeKind.Utc).ToString("o") : null;
            }
            if (local.ListTrelloId != trello.ListTrelloId && local.ListTrelloId != null)
            {
                fields["idList"] = local.ListTrelloId;
                fields["pos"] = "bottom";
            }

            if (fields.Count > 0)
                await _svc._trello.UpdateCardAsync(tc.Id, fields, _ct);
        }

        /* ------------------------------- helpers ------------------------------- */

        private CardState LocalState(CardResponse lc) => new(
            NormalizeText(lc.CardTitle),
            NormalizeText(lc.Description),
            DueKey(ToUtc(lc.DueDate)),
            lc.ListID is int id && _listLocalToTrello.TryGetValue(id, out var trelloListId) ? trelloListId : null);

        private static CardState TrelloState(TrelloCard tc) => new(
            NormalizeText(tc.Name),
            NormalizeText(tc.Desc),
            DueKey(tc.Due?.UtcDateTime),
            tc.IdList);

        private static string DueKey(DateTime? utc) =>
            utc.HasValue ? utc.Value.ToString("yyyy-MM-ddTHH:mm") : string.Empty;

        // AiDesk stores due dates as the user's wall-clock time (no zone); Trello uses UTC.
        private DateTime? ToUtc(DateTime? local) =>
            local.HasValue
                ? TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local.Value, DateTimeKind.Unspecified), _svc._timeZone)
                : null;

        private DateTime? ToLocal(DateTimeOffset? utc) =>
            utc.HasValue
                ? DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc.Value.UtcDateTime, _svc._timeZone), DateTimeKind.Unspecified)
                : null;

        private string DescribeChanges(CardState from, CardState to)
        {
            var parts = new List<string>();
            if (from.Title != to.Title) parts.Add("title");
            if (from.Desc != to.Desc) parts.Add("description");
            if (from.Due != to.Due) parts.Add("due date");
            if (from.ListTrelloId != to.ListTrelloId)
            {
                var target = to.ListTrelloId != null && _trelloListNames.TryGetValue(to.ListTrelloId, out var name) ? name : "another list";
                parts.Add($"moved to \"{target}\"");
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "details";
        }

        private void Detail(string message)
        {
            if (_result.Details.Count < MaxDetails) _result.Details.Add(message);
        }

        private string BuildSummary()
        {
            var r = _result;
            var lists = $"Lists: {r.ListsCreatedInAiDesk} new in AiDesk, {r.ListsCreatedInTrello} new in Trello, {r.ListsLinked} matched";
            var cards = $"Cards: {r.CardsCreatedInAiDesk} new in AiDesk, {r.CardsCreatedInTrello} new in Trello, " +
                        $"{r.CardsUpdatedInAiDesk} updated in AiDesk, {r.CardsUpdatedInTrello} updated in Trello, " +
                        $"{r.CardsLinked} matched by name, {r.CardsUnchanged} unchanged, {r.CardsSkipped} skipped";
            return $"{(r.DryRun ? "Preview - " : string.Empty)}{lists}. {cards}.";
        }
    }
}
