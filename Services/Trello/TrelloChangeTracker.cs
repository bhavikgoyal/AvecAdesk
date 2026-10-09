using AvecADeskApi.DTOs.Trello;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;

namespace AvecADeskApi.Services.Trello;

/// <summary>
/// Used by the checklist and comment controllers to push AiDesk changes to Trello.
/// </summary>
public class TrelloChangeTracker
{
    private readonly ITrelloSyncRepository _repo;
    private readonly TrelloSyncQueue _queue;
    private readonly LogHelper _logHelper;

    public TrelloChangeTracker(ITrelloSyncRepository repo, TrelloSyncQueue queue, LogHelper logHelper)
    {
        _repo = repo;
        _queue = queue;
        _logHelper = logHelper;
    }

    public void CardChanged(int cardId) => _queue.LocalCardChanged(cardId);

    public async Task ChangedAsync(string entityType, int localId)
    {
        var reference = await GetRefAsync(entityType, localId);
        if (reference != null) _queue.LocalCardChanged(reference.CardID);
    }

    public async Task<TrelloLocalRef?> GetRefAsync(string entityType, int localId)
    {
        try
        {
            return await _repo.GetLocalRefAsync(entityType, localId);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(TrelloChangeTracker)}.{nameof(GetRefAsync)} {entityType} {localId}", ex);
            return null;
        }
    }

    public async Task DeletedAsync(string entityType, TrelloLocalRef? reference)
    {
        if (reference == null) return;

        if (reference.TrelloID != null)
        {
            try
            {
                await _repo.AddTombstoneAsync(entityType, reference.TrelloID, reference.TrelloParentID, reference.BoardID);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloChangeTracker)}.{nameof(DeletedAsync)} {entityType} {reference.TrelloID}", ex);
            }
        }

        _queue.LocalCardChanged(reference.CardID);
    }
}
