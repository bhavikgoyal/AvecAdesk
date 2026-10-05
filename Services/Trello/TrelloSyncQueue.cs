using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AvecADeskApi.Services.Trello;

public enum TrelloJobKind
{
    /// <summary>Link/create boards that exist on only one side.</summary>
    Discover,
    LocalBoardChanged,
    LocalCardChanged,
    LocalBoardRenamed,
    TrelloBoardChanged,
}

public readonly record struct TrelloSyncJob(TrelloJobKind Kind, int LocalId = 0, string? TrelloId = null)
{
    public string Key => $"{Kind}:{LocalId}:{TrelloId}";
}

/// <summary>
/// In-memory queue between the API (controllers, Trello webhook) and <see cref="TrelloSyncWorker"/>.
/// A job that is already waiting is not queued twice, so a burst of edits on one board
/// results in a single sync.
/// </summary>
public class TrelloSyncQueue
{
    private readonly Channel<TrelloSyncJob> _channel =
        Channel.CreateUnbounded<TrelloSyncJob>(new UnboundedChannelOptions { SingleReader = true });

    private readonly ConcurrentDictionary<string, byte> _pending = new();

    public ChannelReader<TrelloSyncJob> Reader => _channel.Reader;

    public void Enqueue(TrelloSyncJob job)
    {
        if (_pending.TryAdd(job.Key, 0))
            _channel.Writer.TryWrite(job);
    }

    public void Discover() => Enqueue(new TrelloSyncJob(TrelloJobKind.Discover));

    public void LocalBoardChanged(int boardId)
    {
        if (boardId > 0) Enqueue(new TrelloSyncJob(TrelloJobKind.LocalBoardChanged, boardId));
    }

    public void LocalCardChanged(int cardId)
    {
        if (cardId > 0) Enqueue(new TrelloSyncJob(TrelloJobKind.LocalCardChanged, cardId));
    }

    public void LocalBoardRenamed(int boardId)
    {
        if (boardId > 0) Enqueue(new TrelloSyncJob(TrelloJobKind.LocalBoardRenamed, boardId));
    }

    public void TrelloBoardChanged(string trelloBoardId)
    {
        if (!string.IsNullOrWhiteSpace(trelloBoardId))
            Enqueue(new TrelloSyncJob(TrelloJobKind.TrelloBoardChanged, TrelloId: trelloBoardId));
    }

    /// <summary>Called when the worker picks a job up; later changes queue it again.</summary>
    public void MarkStarted(TrelloSyncJob job) => _pending.TryRemove(job.Key, out _);
}
