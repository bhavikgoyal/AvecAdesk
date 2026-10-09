using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AvecADeskApi.DTOs.Card;
using AvecADeskApi.DTOs.Trello;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;

namespace AvecADeskApi.Services.Trello;

public class TrelloSyncBusyException : Exception
{
    public TrelloSyncBusyException() : base("A sync for this board is already running. Try again in a moment.") { }
}

public class TrelloSyncService
{
    private const int MaxDetails = 100;
    private const int MoveToEndPosition = 999999;
    private const int CommentPageSize = 1000;
    private const int MaxCommentPages = 5;
    private const int MaxLocalNameLength = 255;
    private const int MaxTrelloCommentLength = 16384;
    private const int ActivityPageSize = 1000;
    private const int MaxActivityPages = 5;

    private static readonly string[] TrelloActivityTypes =
    {
        "updateCheckItemStateOnCard", "addChecklistToCard", "removeChecklistFromCard",
        "addAttachmentToCard", "deleteAttachmentFromCard", "addMemberToCard", "removeMemberFromCard",
        "createCard", "copyCard", "updateCard",
    };

    private static readonly ConcurrentDictionary<int, SemaphoreSlim> BoardLocks = new();

    // AiDesk comments are posted with the Trello token's account, prefixed with the real author.
    private static readonly Regex PushedCommentPrefix = new(@"^\*\*(?<name>[^*\r\n]{1,150}):\*\* (?<text>[\s\S]*)$", RegexOptions.Compiled);
    private static string? _tokenMemberId;

    private readonly TrelloClient _trello;
    private readonly ITrelloSyncRepository _repo;
    private readonly IListRepository _lists;
    private readonly ICardRepository _cards;
    private readonly LogHelper _logHelper;
    private readonly TimeZoneInfo _timeZone;

    public TrelloSyncService(
        TrelloClient trello,
        ITrelloSyncRepository repo,
        IListRepository lists,
        ICardRepository cards,
        LogHelper logHelper,
        IConfiguration configuration)
    {
        _trello = trello;
        _repo = repo;
        _lists = lists;
        _cards = cards;
        _logHelper = logHelper;
        _timeZone = ResolveTimeZone(configuration["Trello:TimeZoneId"]);
    }

    private async Task<string> GetTokenMemberIdAsync(CancellationToken ct) =>
        _tokenMemberId ??= (await _trello.GetMeAsync(ct)).Id;

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

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;

    private static string Hash32(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..32];

    private static string ItemHash(string name, bool complete) =>
        Hash32($"{NormalizeText(name)}\u001f{(complete ? 1 : 0)}");

    private static string CommentHash(string? text) => Hash32(NormalizeText(text));

    /// <summary>Trello IDs start with the creation time (Unix seconds, 8 hex chars).</summary>
    private static DateTime? TrelloIdTimeUtc(string? id)
    {
        if (id is not { Length: >= 8 }
            || !long.TryParse(id[..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var seconds))
            return null;
        return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
    }

    /// <summary>Trello ignores null for due/start; an empty string clears them.</summary>
    private static string TrelloDate(DateTime? utc) =>
        utc.HasValue ? DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToString("o") : string.Empty;

    private static readonly (string Family, string Light, string Normal, string Dark)[] LabelPalette =
    {
        ("green", "#BAF3DB", "#4BCE97", "#1F845A"),
        ("yellow", "#F8E6A0", "#F5CD47", "#946F00"),
        ("orange", "#FEDEC8", "#FEA362", "#C25100"),
        ("red", "#FFD5D2", "#F87168", "#C9372C"),
        ("purple", "#DFD8FD", "#9F8FEF", "#6E5DC6"),
        ("blue", "#CCE0FF", "#579DFF", "#0C66E4"),
        ("sky", "#C6EDFB", "#6CC3E0", "#227D9B"),
        ("lime", "#D3F1A7", "#94C748", "#5B7F24"),
        ("pink", "#FDD0EC", "#E774BB", "#AE4787"),
        ("black", "#DCDFE4", "#8590A2", "#626F86"),
    };

    private static readonly Dictionary<string, string> TrelloLabelColorToHex = LabelPalette
        .SelectMany(p => new[] { ($"{p.Family}_light", p.Light), (p.Family, p.Normal), ($"{p.Family}_dark", p.Dark) })
        .ToDictionary(x => x.Item1, x => x.Item2, StringComparer.OrdinalIgnoreCase);

    private static string? ToTrelloLabelColor(string? hex)
    {
        var rgb = ParseHex(hex);
        if (rgb == null) return null;

        string? best = null;
        var bestDistance = int.MaxValue;
        foreach (var (name, paletteHex) in TrelloLabelColorToHex)
        {
            var p = ParseHex(paletteHex)!.Value;
            var distance = (p.R - rgb.Value.R) * (p.R - rgb.Value.R)
                           + (p.G - rgb.Value.G) * (p.G - rgb.Value.G)
                           + (p.B - rgb.Value.B) * (p.B - rgb.Value.B);
            if (distance < bestDistance)
            {
                best = name;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static string ToLocalLabelColor(string? trelloColor) =>
        trelloColor != null && TrelloLabelColorToHex.TryGetValue(trelloColor, out var hex) ? hex : string.Empty;

    private static (int R, int G, int B)? ParseHex(string? hex)
    {
        var clean = (hex ?? string.Empty).Trim().TrimStart('#');
        if (clean.Length == 3) clean = string.Concat(clean.Select(ch => $"{ch}{ch}"));
        if (clean.Length != 6 || !int.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return null;
        return ((value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF);
    }

    private static bool IsPermanentClientError(TrelloApiException ex) =>
        (int)ex.StatusCode is >= 400 and < 500 && ex.StatusCode != HttpStatusCode.TooManyRequests;

    private enum SyncDirection { None, Pull, Push }

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
        private readonly List<(int LocalCardId, string TrelloCardId)> _cardPairs = new();
        private readonly HashSet<string> _deletedInAiDesk = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TrelloCard> _trelloCards = new(StringComparer.Ordinal);
        private readonly Dictionary<int, string> _labelLocalToTrello = new();
        private readonly Dictionary<string, int> _labelTrelloToLocal = new(StringComparer.Ordinal);

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

            if (_cardPairs.Count > 0)
            {
                await RunSectionAsync("Deletes", ProcessTombstonesAsync);
                await RunSectionAsync("Labels", () => SyncBoardLabelsAsync(maps.Where(m => m.EntityType == TrelloEntityTypes.Label).ToList()));
                await RunSectionAsync("Card details", () => SyncCardDetailsAsync(maps));
                await RunSectionAsync("Checklists", SyncChecklistsAsync);
                await RunSectionAsync("Comments", SyncCommentsAsync);
                await RunSectionAsync("Activity", SyncActivityAsync);
            }

            _result.Summary = BuildSummary();
            return _result;
        }

        private async Task RunSectionAsync(string name, Func<Task> section)
        {
            try
            {
                await section();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _result.Errors.Add($"{name}: {ex.Message}");
                _svc._logHelper.LogError($"{nameof(TrelloSyncService)}.{name} board {BoardId}", ex);
            }
        }


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
            foreach (var tc in trelloCards) _trelloCards[tc.Id] = tc;
            var localById = localCards.ToDictionary(c => c.CardID);
            var mappedLocalIds = cardMaps.Select(m => m.LocalID).ToHashSet();
            var mappedTrelloIds = cardMaps.Select(m => m.TrelloID).ToHashSet();

            foreach (var m in cardMaps)
            {
                var hasLocal = localById.TryGetValue(m.LocalID, out var lc);
                var hasTrello = trelloById.TryGetValue(m.TrelloID, out var tc);
                if (hasLocal && hasTrello)
                {
                    await ReconcileAsync(lc!, tc!, m.SyncHash);
                    _cardPairs.Add((lc!.CardID, tc!.Id));
                }
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
                var candidates = unmappedLocal.Where(lc => SameText(lc.CardTitle, tc.Name)).ToList();
                var match = candidates.FirstOrDefault(lc => lc.ListID == localListId)
                            ?? (candidates.Count == 1 ? candidates[0] : null);
                if (match != null)
                {
                    unmappedLocal.Remove(match);
                    _result.CardsLinked++;
                    await ReconcileAsync(match, tc, null);
                    _cardPairs.Add((match.CardID, tc.Id));
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
                    _cardPairs.Add((newCardId, tc.Id));
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
                    _cardPairs.Add((lc.CardID, created.Id));
                    _trelloCards[created.Id] = created;
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
                fields["due"] = TrelloDate(dueUtc);
            }
            if (local.ListTrelloId != trello.ListTrelloId && local.ListTrelloId != null)
            {
                fields["idList"] = local.ListTrelloId;
                fields["pos"] = "bottom";
            }

            if (fields.Count > 0)
                await _svc._trello.UpdateCardAsync(tc.Id, fields, _ct);
        }

        private bool _labelsSynced;

        private static string LabelHash(string? name, string? trelloColor) =>
            Hash32($"{NormalizeText(name)}\u001f{trelloColor?.ToLowerInvariant() ?? string.Empty}");

        /// <summary>
        /// Board labels: name/color both ways (Trello wins conflicts), a label deleted on one side is
        /// </summary>
        private async Task SyncBoardLabelsAsync(List<TrelloMapRow> labelMaps)
        {
            var trelloLabels = await _svc._trello.GetBoardLabelsAsync(_link.TrelloBoardID, _ct);
            var localLabels = await _svc._repo.GetBoardLabelsAsync(BoardId);
            var trelloById = trelloLabels.ToDictionary(l => l.Id, StringComparer.Ordinal);
            var localById = localLabels.ToDictionary(l => l.BoardLabelID);
            var mappedLocal = labelMaps.Select(m => m.LocalID).ToHashSet();
            var mappedTrello = labelMaps.Select(m => m.TrelloID).ToHashSet(StringComparer.Ordinal);

            foreach (var m in labelMaps)
            {
                var hasLocal = localById.TryGetValue(m.LocalID, out var ll);
                var hasTrello = trelloById.TryGetValue(m.TrelloID, out var tl);
                if (hasLocal && hasTrello)
                {
                    await ReconcileLabelAsync(ll!, tl!, m.SyncHash);
                }
                else if (hasLocal)
                {
                    _result.LabelChangesInAiDesk++;
                    Detail($"Label \"{ll!.LabelName}\": {(_dryRun ? "will be deleted" : "deleted")} in AiDesk (removed in Trello)");
                    if (!_dryRun) await _svc._repo.DeleteBoardLabelAsync(ll.BoardLabelID);
                }
                else if (hasTrello)
                {
                    _result.LabelChangesInTrello++;
                    Detail($"Label \"{tl!.Name}\": {(_dryRun ? "will be deleted" : "deleted")} in Trello (removed in AiDesk)");
                    if (_dryRun) continue;
                    try
                    {
                        await _svc._trello.DeleteLabelAsync(tl.Id, _ct);
                    }
                    catch (TrelloApiException ex) when (IsPermanentClientError(ex))
                    {
                        Detail($"Label \"{tl.Name}\": could not be deleted in Trello ({ex.Message})");
                    }
                }
            }

            var unmappedLocal = localLabels.Where(l => !mappedLocal.Contains(l.BoardLabelID)).ToList();

            foreach (var tl in trelloLabels.Where(l => !mappedTrello.Contains(l.Id)))
            {
                var match = unmappedLocal.FirstOrDefault(l => SameText(l.LabelName, tl.Name)
                                                              && string.Equals(ToTrelloLabelColor(l.Color), tl.Color, StringComparison.OrdinalIgnoreCase))
                            ?? (string.IsNullOrWhiteSpace(tl.Name) ? null : unmappedLocal.FirstOrDefault(l => SameText(l.LabelName, tl.Name)));
                if (match != null)
                {
                    unmappedLocal.Remove(match);
                    await ReconcileLabelAsync(match, tl, null);
                    continue;
                }

                var name = Truncate(NormalizeText(tl.Name), 200);
                var localId = _dryRun ? _fakeLocalId-- : await _svc._repo.SaveBoardLabelAsync(null, BoardId, name, ToLocalLabelColor(tl.Color));
                if (!_dryRun)
                {
                    var hash = LabelHash(tl.Name, tl.Color);
                    await _svc._repo.SaveMapAsync(TrelloEntityTypes.Label, localId, tl.Id, BoardId, hash + hash);
                }
                LinkLabel(localId, tl.Id);
                _result.LabelChangesInAiDesk++;
                Detail($"Label \"{name}\": {(_dryRun ? "will be created" : "created")} in AiDesk");
            }

            foreach (var ll in unmappedLocal)
            {
                var color = ToTrelloLabelColor(ll.Color);
                var trelloId = _dryRun
                    ? $"new-label-{_fakeTrelloId++}"
                    : (await _svc._trello.CreateLabelAsync(_link.TrelloBoardID, ll.LabelName, color, _ct)).Id;
                if (!_dryRun)
                {
                    var hash = LabelHash(ll.LabelName, color);
                    await _svc._repo.SaveMapAsync(TrelloEntityTypes.Label, ll.BoardLabelID, trelloId, BoardId, hash + hash);
                }
                LinkLabel(ll.BoardLabelID, trelloId);
                _result.LabelChangesInTrello++;
                Detail($"Label \"{ll.LabelName}\": {(_dryRun ? "will be created" : "created")} in Trello");
            }

            _labelsSynced = true;
        }

        private async Task ReconcileLabelAsync(LocalBoardLabelRow ll, TrelloLabel tl, string? stored)
        {
            var localColor = ToTrelloLabelColor(ll.Color);
            var localHash = LabelHash(ll.LabelName, localColor);
            var trelloHash = LabelHash(tl.Name, tl.Color);
            var newStored = localHash + trelloHash;

            switch (Decide(localHash, trelloHash, stored, sameContent: localHash == trelloHash))
            {
                case SyncDirection.Pull:
                    _result.LabelChangesInAiDesk++;
                    Detail($"Label \"{tl.Name}\": {(_dryRun ? "will be updated" : "updated")} in AiDesk");
                    if (!_dryRun)
                        await _svc._repo.SaveBoardLabelAsync(ll.BoardLabelID, BoardId, Truncate(NormalizeText(tl.Name), 200), ToLocalLabelColor(tl.Color));
                    newStored = trelloHash + trelloHash;
                    break;

                case SyncDirection.Push:
                    _result.LabelChangesInTrello++;
                    Detail($"Label \"{ll.LabelName}\": {(_dryRun ? "will be updated" : "updated")} in Trello");
                    if (!_dryRun)
                        await _svc._trello.UpdateLabelAsync(tl.Id, ll.LabelName, localColor, _ct);
                    newStored = localHash + localHash;
                    break;
            }

            if (!_dryRun && stored != newStored)
                await _svc._repo.SaveMapAsync(TrelloEntityTypes.Label, ll.BoardLabelID, tl.Id, BoardId, newStored);
            LinkLabel(ll.BoardLabelID, tl.Id);
        }

        private void LinkLabel(int localId, string trelloId)
        {
            _labelLocalToTrello[localId] = trelloId;
            _labelTrelloToLocal[trelloId] = localId;
        }


        /// <summary>
        /// last-synced state. Image covers are left alone (Trello images can't be copied without auth).
        /// </summary>
        private async Task SyncCardDetailsAsync(List<TrelloMapRow> maps)
        {
            var stored = maps
                .Where(m => m.EntityType is TrelloEntityTypes.CardStart or TrelloEntityTypes.CardLabels or TrelloEntityTypes.CardCover)
                .GroupBy(m => (m.EntityType, m.LocalID))
                .ToDictionary(g => g.Key, g => g.First().SyncHash);
            var localDetails = (await _svc._repo.GetBoardCardDetailsAsync(BoardId)).ToDictionary(d => d.CardID);
            var localLabels = (await _svc._repo.GetBoardCardLabelsAsync(BoardId))
                .GroupBy(x => x.CardID)
                .ToDictionary(g => g.Key, g => g.Select(x => x.BoardLabelID).ToList());

            foreach (var (localCardId, trelloCardId) in _cardPairs)
            {
                if (!_trelloCards.TryGetValue(trelloCardId, out var tc)) continue;
                localDetails.TryGetValue(localCardId, out var ld);

                try
                {
                    await SyncStartAsync(localCardId, tc, ld?.StartDate, stored.GetValueOrDefault((TrelloEntityTypes.CardStart, localCardId)));
                    if (_labelsSynced)
                        await SyncCardLabelsAsync(localCardId, tc, localLabels.GetValueOrDefault(localCardId) ?? new List<int>(),
                            stored.GetValueOrDefault((TrelloEntityTypes.CardLabels, localCardId)));
                    await SyncCoverAsync(localCardId, tc, ld, stored.GetValueOrDefault((TrelloEntityTypes.CardCover, localCardId)));
                }
                catch (TrelloApiException ex) when (IsPermanentClientError(ex))
                {
                    Detail($"Card {localCardId}: labels/cover/start could not be updated in Trello ({ex.Message})");
                }
            }
        }

        private async Task SyncStartAsync(int localCardId, TrelloCard tc, DateTime? localStart, string? stored)
        {
            var localKey = DueKey(ToUtc(localStart));
            var trelloKey = DueKey(tc.Start?.UtcDateTime);

            switch (DecideDetail(localKey, trelloKey, stored))
            {
                case SyncDirection.Pull:
                    CountDetail(pull: true, localCardId, "start date");
                    if (!_dryRun) await _svc._repo.SetCardStartDateAsync(localCardId, ToLocal(tc.Start));
                    localKey = trelloKey;
                    break;
                case SyncDirection.Push:
                    CountDetail(pull: false, localCardId, "start date");
                    if (!_dryRun)
                        await _svc._trello.UpdateCardAsync(tc.Id, new Dictionary<string, object?> { ["start"] = TrelloDate(ToUtc(localStart)) }, _ct);
                    trelloKey = localKey;
                    break;
            }

            await SaveDetailStateAsync(TrelloEntityTypes.CardStart, localCardId, tc.Id, localKey, trelloKey, stored);
        }

        private async Task SyncCardLabelsAsync(int localCardId, TrelloCard tc, List<int> localLabelIds, string? stored)
        {
            string KeyOf(IEnumerable<string> ids) => string.Join(',', ids.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal));
            IEnumerable<string> ToTrelloIds(IEnumerable<int> ids) =>
                ids.Where(_labelLocalToTrello.ContainsKey).Select(id => _labelLocalToTrello[id]);

            var trelloKnown = tc.IdLabels.Where(_labelTrelloToLocal.ContainsKey).ToList();
            var localKey = KeyOf(ToTrelloIds(localLabelIds));
            var trelloKey = KeyOf(trelloKnown);

            switch (DecideDetail(localKey, trelloKey, stored))
            {
                case SyncDirection.Pull:
                    CountDetail(pull: true, localCardId, "labels");
                    if (_dryRun)
                    {
                        localKey = trelloKey;
                        break;
                    }
                    var applied = await _svc._repo.SetCardLabelsAsync(localCardId, trelloKnown.Select(id => _labelTrelloToLocal[id]));
                    localKey = KeyOf(ToTrelloIds(applied));
                    break;
                case SyncDirection.Push:
                    CountDetail(pull: false, localCardId, "labels");
                    if (!_dryRun)
                    {
                        // Keep Trello labels AiDesk doesn't know about.
                        var unknown = tc.IdLabels.Where(id => !_labelTrelloToLocal.ContainsKey(id));
                        var idLabels = string.Join(',', ToTrelloIds(localLabelIds).Concat(unknown).Distinct(StringComparer.Ordinal));
                        await _svc._trello.UpdateCardAsync(tc.Id, new Dictionary<string, object?> { ["idLabels"] = idLabels }, _ct);
                    }
                    trelloKey = localKey;
                    break;
            }

            await SaveDetailStateAsync(TrelloEntityTypes.CardLabels, localCardId, tc.Id, localKey, trelloKey, stored);
        }

        private async Task SyncCoverAsync(int localCardId, TrelloCard tc, LocalCardDetailsRow? ld, string? stored)
        {
            const string image = "image";
            var localKey = ld?.CoverImageUrl != null ? image
                : ld?.CoverColor != null ? $"{ld.CoverColor.ToLowerInvariant()}|{ld.CoverSize ?? "normal"}"
                : string.Empty;
            var cover = tc.Cover;
            var trelloKey = cover?.IdAttachment != null || cover?.IdUploadedBackground != null ? image
                : cover?.Color != null ? $"{cover.Color.ToLowerInvariant()}|{cover.Size ?? "normal"}"
                : string.Empty;
            if (localKey == image || trelloKey == image) return;

            switch (DecideDetail(localKey, trelloKey, stored))
            {
                case SyncDirection.Pull:
                    CountDetail(pull: true, localCardId, "cover");
                    if (!_dryRun)
                    {
                        if (cover?.Color == null)
                            await _svc._repo.RemoveCardCoverAsync(localCardId);
                        else
                            await _svc._repo.SaveCardCoverAsync(localCardId, cover.Color.ToLowerInvariant(), cover.Size ?? "normal",
                                cover.Brightness == "dark" ? "dark" : "light");
                    }
                    localKey = trelloKey;
                    break;
                case SyncDirection.Push:
                    CountDetail(pull: false, localCardId, "cover");
                    if (!_dryRun)
                    {
                        var trelloCover = ld?.CoverColor == null
                            ? new Dictionary<string, object?> { ["color"] = null, ["idAttachment"] = null, ["idUploadedBackground"] = null }
                            : new Dictionary<string, object?>
                            {
                                ["color"] = ld.CoverColor.ToLowerInvariant(),
                                ["size"] = ld.CoverSize ?? "normal",
                                ["brightness"] = ld.CoverBrightness ?? "light",
                            };
                        await _svc._trello.UpdateCardAsync(tc.Id, new Dictionary<string, object?> { ["cover"] = trelloCover }, _ct);
                    }
                    trelloKey = localKey;
                    break;
            }

            await SaveDetailStateAsync(TrelloEntityTypes.CardCover, localCardId, tc.Id, localKey, trelloKey, stored);
        }

        /// <summary>
        /// so linking a board never wipes labels/covers/dates that exist on only one side.
        /// </summary>
        private SyncDirection DecideDetail(string localKey, string trelloKey, string? stored)
        {
            if (localKey == trelloKey) return SyncDirection.None;
            if (stored is not { Length: 64 })
            {
                if (localKey.Length == 0) return SyncDirection.Pull;
                if (trelloKey.Length == 0) return SyncDirection.Push;
                return _winner == "trello" ? SyncDirection.Pull : SyncDirection.Push;
            }
            return Decide(Hash32(localKey), Hash32(trelloKey), stored, sameContent: false);
        }

        private async Task SaveDetailStateAsync(string entityType, int localCardId, string trelloCardId, string localKey, string trelloKey, string? stored)
        {
            var newStored = Hash32(localKey) + Hash32(trelloKey);
            if (!_dryRun && !string.Equals(stored, newStored, StringComparison.OrdinalIgnoreCase))
                await _svc._repo.SaveMapAsync(entityType, localCardId, trelloCardId, BoardId, newStored);
        }

        private void CountDetail(bool pull, int localCardId, string what)
        {
            if (pull) _result.CardDetailChangesInAiDesk++;
            else _result.CardDetailChangesInTrello++;
            Detail($"Card {localCardId}: {what} {(_dryRun ? "will be updated" : "updated")} in {(pull ? "AiDesk" : "Trello")}");
        }

        /* ------------------------ deletes made in AiDesk ------------------------ */

        private async Task ProcessTombstonesAsync()
        {
            var tombstones = await _svc._repo.GetTombstonesAsync(BoardId);
            foreach (var t in tombstones) _deletedInAiDesk.Add(t.TrelloID);
            if (_dryRun) return;

            foreach (var t in tombstones.Where(t => t.ProcessedAt == null))
            {
                string? error = null;
                try
                {
                    switch (t.EntityType)
                    {
                        case TrelloEntityTypes.Checklist:
                            await _svc._trello.DeleteChecklistAsync(t.TrelloID, _ct);
                            break;
                        case TrelloEntityTypes.CheckItem when t.TrelloParentID != null:
                            await _svc._trello.DeleteCheckItemAsync(t.TrelloParentID, t.TrelloID, _ct);
                            break;
                        case TrelloEntityTypes.Comment:
                            await _svc._trello.DeleteCommentAsync(t.TrelloID, _ct);
                            break;
                        default:
                            error = $"Cannot delete \"{t.EntityType}\" without its Trello parent.";
                            break;
                    }

                    if (error == null)
                    {
                        _result.DeletesSentToTrello++;
                        Detail($"{t.EntityType} {t.TrelloID}: deleted in Trello");
                    }
                }
                catch (TrelloApiException ex) when (IsPermanentClientError(ex))
                {
                    error = ex.Message;
                }

                await _svc._repo.MarkTombstoneAsync(t.TrelloID, error);
            }
        }

        /* ------------------------------ checklists ----------------------------- */

        private async Task SyncChecklistsAsync()
        {
            var trelloByCard = (await _svc._trello.GetBoardChecklistsAsync(_link.TrelloBoardID, _ct))
                .Where(c => !_deletedInAiDesk.Contains(c.Id))
                .GroupBy(c => c.IdCard)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Pos).ToList());
            var localByCard = (await _svc._repo.GetBoardChecklistsAsync(BoardId))
                .GroupBy(c => c.CardID)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (localCardId, trelloCardId) in _cardPairs)
            {
                var trelloLists = trelloByCard.GetValueOrDefault(trelloCardId) ?? new List<TrelloChecklist>();
                var localLists = localByCard.GetValueOrDefault(localCardId) ?? new List<LocalChecklistRow>();
                if (trelloLists.Count == 0 && localLists.Count == 0) continue;

                await SyncCardChecklistsAsync(localCardId, trelloCardId, localLists, trelloLists);
            }
        }

        private async Task SyncCardChecklistsAsync(int localCardId, string trelloCardId,
            List<LocalChecklistRow> localLists, List<TrelloChecklist> trelloLists)
        {
            var unpairedTrello = trelloLists.ToDictionary(c => c.Id);
            var unpairedLocal = new List<LocalChecklistRow>();
            var pairs = new List<(LocalChecklistRow Local, TrelloChecklist Trello)>();

            foreach (var lc in localLists)
            {
                if (lc.TrelloChecklistID == null)
                {
                    unpairedLocal.Add(lc);
                }
                else if (unpairedTrello.Remove(lc.TrelloChecklistID, out var tc))
                {
                    pairs.Add((lc, tc));
                }
                else
                {
                    if (!_dryRun) await _svc._repo.DeleteLocalAsync(TrelloEntityTypes.Checklist, lc.ChecklistID);
                    _result.ChecklistChangesInAiDesk++;
                    Detail($"Checklist \"{lc.ChecklistTitle}\": {(_dryRun ? "will be deleted" : "deleted")} in AiDesk (removed in Trello)");
                }
            }

            foreach (var tc in trelloLists.Where(c => unpairedTrello.ContainsKey(c.Id)))
            {
                var match = unpairedLocal.FirstOrDefault(lc => SameText(lc.ChecklistTitle, tc.Name));
                if (match != null)
                {
                    unpairedLocal.Remove(match);
                    pairs.Add((match, tc));
                    continue;
                }

                _result.ChecklistChangesInAiDesk++;
                Detail($"Checklist \"{tc.Name}\": {(_dryRun ? "will be created" : "created")} in AiDesk");
                if (_dryRun) continue;

                var title = Truncate(tc.Name, MaxLocalNameLength);
                var newId = await _svc._repo.SaveChecklistAsync(null, localCardId, title, tc.Id, ToLocal(TrelloIdTimeUtc(tc.Id)));
                pairs.Add((new LocalChecklistRow { ChecklistID = newId, CardID = localCardId, ChecklistTitle = title, TrelloChecklistID = tc.Id }, tc));
            }

            foreach (var lc in unpairedLocal)
            {
                _result.ChecklistChangesInTrello++;
                Detail($"Checklist \"{lc.ChecklistTitle}\": {(_dryRun ? "will be created" : "created")} in Trello");
                if (_dryRun) continue;

                var created = await _svc._trello.CreateChecklistAsync(trelloCardId, lc.ChecklistTitle, _ct);
                await _svc._repo.SaveChecklistAsync(lc.ChecklistID, localCardId, lc.ChecklistTitle, created.Id, null);
                lc.TrelloChecklistID = created.Id;
                created.Name = lc.ChecklistTitle;
                created.CheckItems = new List<TrelloCheckItem>();
                pairs.Add((lc, created));
            }

            foreach (var (lc, tc) in pairs)
            {
                var title = Truncate(tc.Name, MaxLocalNameLength);
                if (!_dryRun && (lc.TrelloChecklistID != tc.Id || lc.ChecklistTitle != title))
                    await _svc._repo.SaveChecklistAsync(lc.ChecklistID, localCardId, title, tc.Id, null);

                await SyncCheckItemsAsync(lc, tc, trelloCardId);
            }
        }

        private async Task SyncCheckItemsAsync(LocalChecklistRow lc, TrelloChecklist tc, string trelloCardId)
        {
            var trelloItems = tc.CheckItems
                .Where(i => !_deletedInAiDesk.Contains(i.Id))
                .OrderBy(i => i.Pos)
                .ToList();
            var positions = trelloItems
                .Select((item, index) => (item.Id, Position: index + 1))
                .ToDictionary(x => x.Id, x => x.Position);
            var unpairedTrello = trelloItems.ToDictionary(i => i.Id);
            var unpairedLocal = new List<LocalCheckItemRow>();

            foreach (var li in lc.Items)
            {
                if (li.TrelloItemID == null)
                {
                    unpairedLocal.Add(li);
                }
                else if (unpairedTrello.Remove(li.TrelloItemID, out var ti))
                {
                    await ReconcileCheckItemAsync(lc.ChecklistID, li, ti, positions[ti.Id], trelloCardId);
                }
                else
                {
                    if (!_dryRun) await _svc._repo.DeleteLocalAsync(TrelloEntityTypes.CheckItem, li.ChecklistItemID);
                    _result.ChecklistChangesInAiDesk++;
                    Detail($"Checklist item \"{li.ItemName}\": {(_dryRun ? "will be deleted" : "deleted")} in AiDesk (removed in Trello)");
                }
            }

            foreach (var ti in trelloItems.Where(i => unpairedTrello.ContainsKey(i.Id)))
            {
                var match = unpairedLocal.FirstOrDefault(li => SameText(li.ItemName, ti.Name));
                if (match != null)
                {
                    unpairedLocal.Remove(match);
                    await ReconcileCheckItemAsync(lc.ChecklistID, match, ti, positions[ti.Id], trelloCardId);
                    continue;
                }

                _result.ChecklistChangesInAiDesk++;
                Detail($"Checklist item \"{ti.Name}\": {(_dryRun ? "will be created" : "created")} in AiDesk");
                if (_dryRun) continue;

                var name = Truncate(ti.Name, MaxLocalNameLength);
                await _svc._repo.SaveChecklistItemAsync(null, lc.ChecklistID, name, ti.IsComplete, positions[ti.Id], ti.Id,
                    ItemHash(name, ti.IsComplete) + ItemHash(ti.Name, ti.IsComplete), ToLocal(TrelloIdTimeUtc(ti.Id)));
            }

            foreach (var li in unpairedLocal)
            {
                _result.ChecklistChangesInTrello++;
                Detail($"Checklist item \"{li.ItemName}\": {(_dryRun ? "will be created" : "created")} in Trello");
                if (_dryRun) continue;

                var created = await _svc._trello.CreateCheckItemAsync(tc.Id, li.ItemName, li.IsCompleted, _ct);
                await _svc._repo.SaveChecklistItemAsync(li.ChecklistItemID, lc.ChecklistID, li.ItemName, li.IsCompleted, null, created.Id,
                    ItemHash(li.ItemName, li.IsCompleted) + ItemHash(created.Name, created.IsComplete), null);
            }
        }

        private async Task ReconcileCheckItemAsync(int checklistId, LocalCheckItemRow li, TrelloCheckItem ti, int position, string trelloCardId)
        {
            var localHash = ItemHash(li.ItemName, li.IsCompleted);
            var trelloHash = ItemHash(ti.Name, ti.IsComplete);

            switch (Decide(localHash, trelloHash, li.SyncHash, sameContent: localHash == trelloHash))
            {
                case SyncDirection.Pull:
                    _result.ChecklistChangesInAiDesk++;
                    Detail($"Checklist item \"{ti.Name}\": {(_dryRun ? "will be updated" : "updated")} in AiDesk");
                    if (_dryRun) break;

                    var name = Truncate(ti.Name, MaxLocalNameLength);
                    await _svc._repo.SaveChecklistItemAsync(li.ChecklistItemID, checklistId, name, ti.IsComplete, position, ti.Id,
                        ItemHash(name, ti.IsComplete) + trelloHash, null);
                    break;

                case SyncDirection.Push:
                    _result.ChecklistChangesInTrello++;
                    Detail($"Checklist item \"{li.ItemName}\": {(_dryRun ? "will be updated" : "updated")} in Trello");
                    if (_dryRun) break;

                    var updated = await _svc._trello.UpdateCheckItemAsync(trelloCardId, ti.Id, li.ItemName, li.IsCompleted, _ct);
                    await _svc._repo.SaveChecklistItemAsync(li.ChecklistItemID, checklistId, li.ItemName, li.IsCompleted, position, ti.Id,
                        localHash + ItemHash(updated.Name, updated.IsComplete), null);
                    break;

                default:
                    var syncHash = localHash + trelloHash;
                    if (!_dryRun && (li.SyncHash != syncHash || li.TrelloItemID != ti.Id || li.Position != position))
                        await _svc._repo.SaveChecklistItemAsync(li.ChecklistItemID, checklistId, li.ItemName, li.IsCompleted, position, ti.Id,
                            syncHash, null);
                    break;
            }
        }

        /* ------------------------------- comments ------------------------------ */

        private async Task SyncCommentsAsync()
        {
            var (trelloComments, complete) = await FetchTrelloCommentsAsync();
            var oldestFetchedUtc = complete ? (DateTime?)null : TrelloIdTimeUtc(trelloComments[^1].Id);

            var trelloByCard = trelloComments
                .Where(a => a.Data?.Card?.Id != null && !_deletedInAiDesk.Contains(a.Id))
                .GroupBy(a => a.Data!.Card!.Id)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Date).ToList());
            var localByCard = (await _svc._repo.GetBoardCommentsAsync(BoardId))
                .GroupBy(c => c.CardID)
                .ToDictionary(g => g.Key, g => g.ToList());
            if (trelloByCard.Count == 0 && localByCard.Count == 0) return;

            var users = await _svc._repo.GetTrelloUserMapAsync();
            var tokenMemberId = await _svc.GetTokenMemberIdAsync(_ct);

            foreach (var (localCardId, trelloCardId) in _cardPairs)
            {
                var trelloList = trelloByCard.GetValueOrDefault(trelloCardId) ?? new List<TrelloCommentAction>();
                var localList = localByCard.GetValueOrDefault(localCardId) ?? new List<LocalCommentRow>();
                if (trelloList.Count == 0 && localList.Count == 0) continue;

                var unpairedTrello = trelloList.ToDictionary(a => a.Id);
                var unpairedLocal = new List<LocalCommentRow>();

                foreach (var lc in localList)
                {
                    if (lc.TrelloCommentID == null)
                    {
                        unpairedLocal.Add(lc);
                    }
                    else if (unpairedTrello.Remove(lc.TrelloCommentID, out var ta))
                    {
                        await ReconcileCommentAsync(lc, ta, users, tokenMemberId);
                    }
                    else if (oldestFetchedUtc == null || TrelloIdTimeUtc(lc.TrelloCommentID) >= oldestFetchedUtc)
                    {
                        if (!_dryRun) await _svc._repo.DeleteLocalAsync(TrelloEntityTypes.Comment, lc.CommentID);
                        _result.CommentChangesInAiDesk++;
                        Detail($"Comment {lc.CommentID}: {(_dryRun ? "will be deleted" : "deleted")} in AiDesk (removed in Trello)");
                    }
                }

                foreach (var ta in trelloList.Where(a => unpairedTrello.ContainsKey(a.Id)))
                {
                    int? authorId = ta.IdMemberCreator != null && users.TryGetValue(ta.IdMemberCreator, out var id) ? id : null;
                    var text = ToLocalText(ta, authorId.HasValue, tokenMemberId);
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    _result.CommentChangesInAiDesk++;
                    Detail($"Comment on Trello card {trelloCardId}: {(_dryRun ? "will be created" : "created")} in AiDesk");
                    if (_dryRun) continue;

                    await _svc._repo.SaveCommentAsync(null, localCardId, authorId ?? _userId, text, ta.Date.UtcDateTime, ta.Id,
                        CommentHash(text) + CommentHash(ta.Data?.Text));
                }

                foreach (var lc in unpairedLocal)
                {
                    _result.CommentChangesInTrello++;
                    Detail($"Comment {lc.CommentID}: {(_dryRun ? "will be created" : "created")} in Trello");
                    if (_dryRun) continue;

                    var trelloText = ToTrelloText(lc, tokenMemberId);
                    var created = await _svc._trello.AddCommentAsync(trelloCardId, trelloText, _ct);
                    await _svc._repo.SaveCommentAsync(lc.CommentID, lc.CardID, lc.UserID, lc.CommentText, null, created.Id,
                        CommentHash(lc.CommentText) + CommentHash(created.Data?.Text ?? trelloText));
                }
            }
        }

        private async Task<(List<TrelloCommentAction> Comments, bool Complete)> FetchTrelloCommentsAsync()
        {
            var all = new List<TrelloCommentAction>();
            string? before = null;
            for (var page = 0; page < MaxCommentPages; page++)
            {
                var batch = await _svc._trello.GetBoardCommentsAsync(_link.TrelloBoardID, before, CommentPageSize, _ct);
                all.AddRange(batch);
                if (batch.Count < CommentPageSize) return (all, true);
                before = batch[^1].Id;
            }
            return (all, false);
        }

        private async Task ReconcileCommentAsync(LocalCommentRow lc, TrelloCommentAction ta, Dictionary<string, int> users, string tokenMemberId)
        {
            var localHash = CommentHash(lc.CommentText);
            var trelloHash = CommentHash(ta.Data?.Text);

            switch (Decide(localHash, trelloHash, lc.SyncHash, sameContent: false))
            {
                case SyncDirection.Pull:
                    var authorMapped = ta.IdMemberCreator != null && users.ContainsKey(ta.IdMemberCreator);
                    var text = ToLocalText(ta, authorMapped, tokenMemberId);
                    if (string.IsNullOrWhiteSpace(text)) break;

                    _result.CommentChangesInAiDesk++;
                    Detail($"Comment {lc.CommentID}: {(_dryRun ? "will be updated" : "updated")} in AiDesk");
                    if (_dryRun) break;

                    await _svc._repo.SaveCommentAsync(lc.CommentID, lc.CardID, lc.UserID, text, null, ta.Id, CommentHash(text) + trelloHash);
                    break;

                case SyncDirection.Push:
                    if (_dryRun)
                    {
                        _result.CommentChangesInTrello++;
                        Detail($"Comment {lc.CommentID}: will be updated in Trello");
                        break;
                    }

                    var sentHash = trelloHash;
                    try
                    {
                        var trelloText = ToTrelloText(lc, tokenMemberId);
                        var updated = await _svc._trello.UpdateCommentAsync(ta.Id, trelloText, _ct);
                        sentHash = CommentHash(updated.Data?.Text ?? trelloText);
                        _result.CommentChangesInTrello++;
                        Detail($"Comment {lc.CommentID}: updated in Trello");
                    }
                    catch (TrelloApiException ex) when (IsPermanentClientError(ex))
                    {
                        Detail($"Comment {lc.CommentID}: could not be updated in Trello ({ex.Message})");
                    }

                    await _svc._repo.SaveCommentAsync(lc.CommentID, lc.CardID, lc.UserID, lc.CommentText, null, ta.Id, localHash + sentHash);
                    break;
            }
        }

        /// <summary>
        /// Comments posted from AiDesk carry a "**Author:** " prefix in Trello, which is removed again here.
        /// </summary>
        private static string ToLocalText(TrelloCommentAction ta, bool authorMapped, string tokenMemberId)
        {
            var text = NormalizeText(ta.Data?.Text);
            if (ta.IdMemberCreator == tokenMemberId)
            {
                var pushed = PushedCommentPrefix.Match(text);
                if (pushed.Success) return pushed.Groups["text"].Value;
            }
            if (authorMapped) return text;

            var name = NullIfBlank(ta.MemberCreator?.FullName) ?? NullIfBlank(ta.MemberCreator?.Username);
            return name == null ? text : $"{name.Trim()}: {text}";
        }

        private static string ToTrelloText(LocalCommentRow lc, string tokenMemberId)
        {
            var text = NormalizeText(lc.CommentText);
            if (lc.AuthorTrelloId != tokenMemberId)
                text = $"**{lc.AuthorName.Replace("*", string.Empty).Trim()}:** {text}";
            return Truncate(text, MaxTrelloCommentLength);
        }

        /* ------------------------------- activity ------------------------------ */

        /// <summary>
        /// the AiDesk activity log. Only actions newer than the last imported one are fetched.
        /// </summary>
        private async Task SyncActivityAsync()
        {
            var since = await _svc._repo.GetLastActivityIdAsync(BoardId);
            var actions = new List<TrelloActivityAction>();
            string? before = null;
            for (var page = 0; page < MaxActivityPages; page++)
            {
                var batch = await _svc._trello.GetBoardActivityAsync(_link.TrelloBoardID, TrelloActivityTypes, since, before, ActivityPageSize, _ct);
                actions.AddRange(batch);
                if (batch.Count < ActivityPageSize) break;
                before = batch[^1].Id;
            }
            if (actions.Count == 0) return;

            var localCardIds = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (localCardId, trelloCardId) in _cardPairs)
                localCardIds.TryAdd(trelloCardId, localCardId);

            var users = await _svc._repo.GetTrelloUserMapAsync();
            var tokenMemberId = await _svc.GetTokenMemberIdAsync(_ct);

            // Oldest first, so an interrupted run never leaves a gap before the last imported ID.
            foreach (var action in actions.OrderBy(a => a.Id, StringComparer.Ordinal))
            {
                if (action.Data?.Card?.Id is not { } trelloCardId || !localCardIds.TryGetValue(trelloCardId, out var localCardId))
                    continue;

                var activity = ToLocalActivity(action);
                if (activity == null) continue;

                int? authorId = action.IdMemberCreator != null && users.TryGetValue(action.IdMemberCreator, out var id) ? id : null;
                activity.CardID = localCardId;
                activity.UserID = authorId ?? _userId;
                activity.TrelloMemberName = authorId.HasValue
                    ? null
                    : NullIfBlank(action.MemberCreator?.FullName) ?? NullIfBlank(action.MemberCreator?.Username) ?? "Trello";
                activity.CreatedAtUtc = action.Date.UtcDateTime;
                activity.TrelloActivityId = action.Id;
                activity.EchoOfAiDesk &= action.IdMemberCreator == tokenMemberId;

                if (_dryRun || await _svc._repo.SaveActivityAsync(activity))
                    _result.ActivitiesImported++;
            }
        }

        /// <summary>
        /// Maps a Trello action to the activity types the AiDesk card panel already shows. Returns null for
        /// </summary>
        private static TrelloActivityImport? ToLocalActivity(TrelloActivityAction action)
        {
            var data = action.Data!;
            switch (action.Type)
            {
                case "updateCheckItemStateOnCard":
                    var state = string.Equals(data.CheckItem?.State, "complete", StringComparison.OrdinalIgnoreCase) ? "complete" : "incomplete";
                    return new TrelloActivityImport
                    {
                        ActivityType = "updateCheckItemStateOnCard",
                        Description = data.CheckItem?.Name,
                        OldValue = state == "complete" ? "incomplete" : "complete",
                        NewValue = state,
                        EchoOfAiDesk = true,
                        EchoDescription = data.CheckItem?.Name,
                        EchoNewValue = state,
                    };
                case "addChecklistToCard":
                    return new TrelloActivityImport
                    {
                        ActivityType = "cardChecklistAdded",
                        Description = data.Checklist?.Name,
                        EchoOfAiDesk = true,
                        EchoDescription = data.Checklist?.Name,
                    };
                case "removeChecklistFromCard":
                    return new TrelloActivityImport
                    {
                        ActivityType = "cardChecklistRemoved",
                        Description = data.Checklist?.Name,
                        EchoOfAiDesk = true,
                        EchoDescription = data.Checklist?.Name,
                    };
                case "addAttachmentToCard":
                    return new TrelloActivityImport { ActivityType = "cardAttachmentAdded", Description = data.Attachment?.Name, NewValue = data.Attachment?.Url };
                case "deleteAttachmentFromCard":
                    return new TrelloActivityImport { ActivityType = "cardAttachmentDeleted", Description = data.Attachment?.Name };
                case "addMemberToCard":
                    return new TrelloActivityImport { ActivityType = "cardMemberAdded", Description = NullIfBlank(action.Member?.FullName) ?? data.Member?.Name };
                case "removeMemberFromCard":
                    return new TrelloActivityImport { ActivityType = "cardMemberRemoved", Description = NullIfBlank(action.Member?.FullName) ?? data.Member?.Name };
                case "createCard":
                    return new TrelloActivityImport { ActivityType = "cardCreated", NewValue = data.List?.Name };
                case "copyCard":
                    return new TrelloActivityImport { ActivityType = "cardCopied", OldValue = data.CardSource?.Name, NewValue = data.List?.Name };
                case "updateCard":
                    return ToLocalCardUpdate(data);
                default:
                    return null;
            }
        }

        private static TrelloActivityImport? ToLocalCardUpdate(TrelloActivityData data)
        {
            if (data.Old is not { ValueKind: System.Text.Json.JsonValueKind.Object } old) return null;

            if (old.TryGetProperty("idList", out _))
                return new TrelloActivityImport
                {
                    ActivityType = "cardMoved",
                    OldValue = data.ListBefore?.Name,
                    NewValue = data.ListAfter?.Name,
                    EchoOfAiDesk = true,
                    EchoNewValue = data.ListAfter?.Name,
                };

            if (old.TryGetProperty("name", out var oldName))
                return new TrelloActivityImport
                {
                    ActivityType = "cardRenamed",
                    OldValue = oldName.ValueKind == System.Text.Json.JsonValueKind.String ? oldName.GetString() : null,
                    NewValue = data.Card?.Name,
                    EchoOfAiDesk = true,
                    EchoNewValue = data.Card?.Name,
                };

            if (old.TryGetProperty("due", out _))
                return data.Card?.Due is { } due
                    ? new TrelloActivityImport
                    {
                        ActivityType = "cardDueDateChanged",
                        NewValue = due.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
                        EchoOfAiDesk = true,
                    }
                    : new TrelloActivityImport { ActivityType = "cardDueDateRemoved", EchoOfAiDesk = true };

            if (old.TryGetProperty("closed", out _))
                return new TrelloActivityImport { ActivityType = data.Card?.Closed == true ? "cardArchived" : "cardUnarchived" };

            return null;
        }

        /// <summary>
        /// With no history the board's chosen side wins; afterwards Trello wins conflicts.
        /// </summary>
        private SyncDirection Decide(string localHash, string trelloHash, string? stored, bool sameContent)
        {
            if (stored is not { Length: 64 })
                return sameContent ? SyncDirection.None : _winner == "trello" ? SyncDirection.Pull : SyncDirection.Push;
            if (sameContent) return SyncDirection.None;
            if (!string.Equals(stored[32..], trelloHash, StringComparison.OrdinalIgnoreCase)) return SyncDirection.Pull;
            if (!string.Equals(stored[..32], localHash, StringComparison.OrdinalIgnoreCase)) return SyncDirection.Push;
            return SyncDirection.None;
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
            var checklists = $"Checklists: {r.ChecklistChangesInAiDesk} changes in AiDesk, {r.ChecklistChangesInTrello} in Trello";
            var comments = $"Comments: {r.CommentChangesInAiDesk} changes in AiDesk, {r.CommentChangesInTrello} in Trello";
            var labels = $"Labels: {r.LabelChangesInAiDesk} changes in AiDesk, {r.LabelChangesInTrello} in Trello";
            var details = $"Card labels/cover/start: {r.CardDetailChangesInAiDesk} changes in AiDesk, {r.CardDetailChangesInTrello} in Trello";
            var summary = $"{(r.DryRun ? "Preview - " : string.Empty)}{lists}. {cards}. {labels}. {details}. {checklists}. {comments}. " +
                          $"Deletes sent to Trello: {r.DeletesSentToTrello}. Trello activity imported: {r.ActivitiesImported}.";
            return r.Errors.Count > 0 ? $"{summary} Errors: {string.Join(" | ", r.Errors)}" : summary;
        }
    }
}
