using AvecADeskApi.LOG;

namespace AvecADeskApi.Services;

/// <summary>
/// Creates Accounting payment-reminder tasks 5 days before a student fees date.
/// </summary>
public class AccountingPaymentReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountingPaymentReminderBackgroundService> _logger;
    private readonly LogHelper _logHelper;
    private DateTime? _lastRunDate;

    public AccountingPaymentReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AccountingPaymentReminderBackgroundService> logger,
        LogHelper logHelper)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _logHelper = logHelper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue("AccountingPaymentReminder:EnableScheduler", true);
        if (!enabled)
        {
            _logger.LogInformation("Accounting payment reminder scheduler is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TryRunAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logHelper.LogError(nameof(AccountingPaymentReminderBackgroundService), ex);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task TryRunAsync()
    {
        var now = DateTime.Now;
        var runHour = _configuration.GetValue("AccountingPaymentReminder:RunHour", 8);

        if (now.Hour < runHour)
            return;

        if (_lastRunDate?.Date == now.Date)
            return;

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AccountingPaymentReminderService>();
        var result = await service.RunAsync();
        _lastRunDate = now.Date;
        _logger.LogInformation("Accounting payment reminder job: {Message}", result.Message);
    }
}
