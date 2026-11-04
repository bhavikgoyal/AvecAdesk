using AvecADeskApi.LOG;

namespace AvecADeskApi.Services;

/// <summary>
/// On the configured days of each month (CommissionReportRequest:SendDays, default 1 and 2) after RunHour,
/// sends the commission report request mail for the previous month. Safe to repeat: an email that already
/// got that month's mail (CommissionReportRequestLog) is skipped, so a contact added later in the window still gets it.
/// </summary>
public class CommissionReportRequestBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommissionReportRequestBackgroundService> _logger;
    private readonly LogHelper _logHelper;
    private DateTime? _lastRunDate;

    public CommissionReportRequestBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<CommissionReportRequestBackgroundService> logger,
        LogHelper logHelper)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _logHelper = logHelper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue($"{CommissionReportRequestService.SectionName}:EnableScheduler", false);
        if (!enabled)
        {
            _logger.LogInformation("Commission report request scheduler is disabled.");
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
                _logHelper.LogError(nameof(CommissionReportRequestBackgroundService), ex);
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task TryRunAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.Now;
        var runHour = _configuration.GetValue($"{CommissionReportRequestService.SectionName}:RunHour", 10);
        var sendDays = ParseDays(_configuration[$"{CommissionReportRequestService.SectionName}:SendDays"]);

        if (!sendDays.Contains(now.Day) || now.Hour < runHour)
            return;

        if (_lastRunDate?.Date == now.Date)
            return;

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CommissionReportRequestService>();
        var result = await service.RunAsync(null, previewOnly: false, force: false, stoppingToken);
        _lastRunDate = now.Date;
        _logger.LogInformation("Commission report request job: {Message}", result.Message);
    }

    private static HashSet<int> ParseDays(string? value)
    {
        var days = (value ?? "1,2")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => int.TryParse(d, out var day) ? day : 0)
            .Where(d => d is >= 1 and <= 31)
            .ToHashSet();
        return days.Count > 0 ? days : new HashSet<int> { 1, 2 };
    }
}
