using AvecADeskApi.LOG;
using AvecADeskApi.Services;

namespace AvecADeskApi.Services;

/// <summary>
/// Runs near month-end and generates/sends the monthly paid-student tax invoice to admin.
/// </summary>
public class MonthlyInvoiceBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MonthlyInvoiceBackgroundService> _logger;
    private readonly LogHelper _logHelper;
    private DateTime? _lastRunDate;

    public MonthlyInvoiceBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<MonthlyInvoiceBackgroundService> logger,
        LogHelper logHelper)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _logHelper = logHelper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue("MonthlyInvoice:EnableScheduler", true);
        if (!enabled)
        {
            _logger.LogInformation("Monthly invoice scheduler is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TryRunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logHelper.LogError(nameof(MonthlyInvoiceBackgroundService), ex);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task TryRunAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.Now;
        var runHour = _configuration.GetValue("MonthlyInvoice:RunHour", 18);
        var lastDay = DateTime.DaysInMonth(now.Year, now.Month);

        if (now.Day != lastDay || now.Hour < runHour)
            return;

        if (_lastRunDate?.Date == now.Date)
            return;

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<MonthlyInvoiceService>();
        var result = await service.GenerateAndSendAsync(
            year: now.Year,
            month: now.Month,
            cancellationToken: stoppingToken);
        _lastRunDate = now.Date;
        _logger.LogInformation("Monthly invoice job: {Message}", result.Message);
    }
}
