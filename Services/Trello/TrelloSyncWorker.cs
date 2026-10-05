using AvecADeskApi.LOG;

namespace AvecADeskApi.Services.Trello;

/// <summary>
/// Runs every Trello sync job one at a time:
///  - jobs queued by AiDesk controllers (board/list/card created or changed) and by the Trello webhook,
///  - a periodic full run as a safety net (Trello:PollIntervalSeconds without a webhook,
///    Trello:BackupPollMinutes when Trello:WebhookCallbackUrl is set).
/// Set Trello:EnableAutoSync = false on any extra API instance that shares the same database,
/// otherwise two instances could create the same board twice.
/// </summary>
public class TrelloSyncWorker : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);
    // Lets a burst of edits (e.g. create card + set due date) collapse into one sync.
    private static readonly TimeSpan CoalesceDelay = TimeSpan.FromSeconds(1);

    private readonly TrelloSyncQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;

    public TrelloSyncWorker(
        TrelloSyncQueue queue,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        LogHelper logHelper)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logHelper = logHelper;
    }

    public static bool IsAutoSyncEnabled(IConfiguration configuration) =>
        configuration.GetValue("Trello:EnableAutoSync", true);

    private bool IsEnabled =>
        IsAutoSyncEnabled(_configuration)
        && !string.IsNullOrWhiteSpace(_configuration["Trello:ApiKey"])
        && !string.IsNullOrWhiteSpace(_configuration["Trello:Token"]);

    private TimeSpan PollInterval =>
        TrelloAutoSyncService.GetWebhookCallbackUrl(_configuration) != null
            ? TimeSpan.FromMinutes(Math.Max(1, _configuration.GetValue("Trello:BackupPollMinutes", 5)))
            : TimeSpan.FromSeconds(Math.Max(15, _configuration.GetValue("Trello:PollIntervalSeconds", 60)));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            if (IsEnabled)
                await RunAsync(s => s.FullRunAsync(stoppingToken));
            var nextFullRun = DateTime.UtcNow + PollInterval;

            while (!stoppingToken.IsCancellationRequested)
            {
                var job = await WaitForJobAsync(nextFullRun - DateTime.UtcNow, stoppingToken);

                if (job is { } j)
                {
                    await Task.Delay(CoalesceDelay, stoppingToken);
                    _queue.MarkStarted(j);
                    if (IsEnabled)
                        await RunAsync(s => s.ProcessAsync(j, stoppingToken));
                }
                else
                {
                    if (IsEnabled)
                        await RunAsync(s => s.FullRunAsync(stoppingToken));
                    nextFullRun = DateTime.UtcNow + PollInterval;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>Returns null when <paramref name="timeout"/> passes without a job.</summary>
    private async Task<TrelloSyncJob?> WaitForJobAsync(TimeSpan timeout, CancellationToken stoppingToken)
    {
        if (timeout <= TimeSpan.Zero) return null;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(timeout);
        try
        {
            return await _queue.Reader.ReadAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task RunAsync(Func<TrelloAutoSyncService, Task> work)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await work(scope.ServiceProvider.GetRequiredService<TrelloAutoSyncService>());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logHelper.LogError(nameof(TrelloSyncWorker), ex);
        }
    }
}
