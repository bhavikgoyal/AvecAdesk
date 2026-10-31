using AvecADeskApi.LOG;

namespace AvecADeskApi.Services;

/// <summary>
/// Every StudentFeeTodoReminder:IntervalSeconds adds the student fees due in
/// StudentFeeTodoReminder:DaysBefore days to that day's card in the configured board list.
/// Safe to repeat: already-added installments are skipped via StudentFeeTodoReminderLog.
/// </summary>
public class StudentFeeTodoReminderBackgroundService : BackgroundService
{
    private const int DefaultIntervalSeconds = 45;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StudentFeeTodoReminderBackgroundService> _logger;
    private readonly LogHelper _logHelper;

    public StudentFeeTodoReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<StudentFeeTodoReminderBackgroundService> logger,
        LogHelper logHelper)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _logHelper = logHelper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue("StudentFeeTodoReminder:EnableScheduler", true);
        if (!enabled)
        {
            _logger.LogInformation("Student fee To Do reminder scheduler is disabled.");
            return;
        }

        var intervalSeconds = Math.Max(_configuration.GetValue("StudentFeeTodoReminder:IntervalSeconds", DefaultIntervalSeconds), 1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logHelper.LogError(nameof(StudentFeeTodoReminderBackgroundService), ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task RunOnceAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<StudentFeeTodoReminderService>();
        var result = await service.RunAsync();

        if (result.CreatedCount > 0)
            _logger.LogInformation("Student fee To Do reminder job: {Message}", result.Message);
    }
}
