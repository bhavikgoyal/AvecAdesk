using AvecADeskApi.LOG;

namespace AvecADeskApi.Services;

/// <summary>
/// Runs once a day: checks contracts expiring within N days (creates
/// renewal tasks for Admin) and checks unpaid invoices (creates/updates
/// follow-up tasks for Admin). Same pattern/config style as your existing
/// MonthlyInvoiceBackgroundService and AccountingPaymentReminder scheduler
/// — each feature has its own EnableScheduler + RunHour in appsettings.json.
/// </summary>
public class TaskAutomationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TaskAutomationBackgroundService> _logger;
    private readonly LogHelper _logHelper;
    private DateTime? _lastContractRunDate;
    private DateTime? _lastInvoiceRunDate;

    public TaskAutomationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<TaskAutomationBackgroundService> logger,
        LogHelper logHelper)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _logHelper = logHelper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TryRunContractExpiryAsync(stoppingToken);
                await TryRunInvoiceDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logHelper.LogError(nameof(TaskAutomationBackgroundService), ex);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task TryRunContractExpiryAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue("ContractExpiryReminder:EnableScheduler", true);
        if (!enabled) return;

        var now = DateTime.Now;
        var runHour = _configuration.GetValue("ContractExpiryReminder:RunHour", 9);
        if (now.Hour < runHour) return;
        if (_lastContractRunDate?.Date == now.Date) return;

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ContractExpiryReminderService>();
        var result = await service.RunAsync();
        _logger.LogInformation("Contract expiry task job: {Message}", result.Message);
        _lastContractRunDate = now.Date;
    }

    private async Task TryRunInvoiceDueAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue("InvoiceDueReminder:EnableScheduler", true);
        if (!enabled) return;

        var now = DateTime.Now;
        var runHour = _configuration.GetValue("InvoiceDueReminder:RunHour", 10);
        if (now.Hour < runHour) return;
        if (_lastInvoiceRunDate?.Date == now.Date) return;

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<InvoiceDueReminderService>();
        var result = await service.RunAsync();
        _logger.LogInformation("Invoice due task job: {Message}", result.Message);
        _lastInvoiceRunDate = now.Date;
    }
}
