using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;

namespace AvecADeskApi.Services;

public class AccountingPaymentReminderService
{
    private readonly IAccountingPaymentReminderRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;

    public AccountingPaymentReminderService(
        IAccountingPaymentReminderRepository repository,
        IConfiguration configuration,
        LogHelper logHelper)
    {
        _repository = repository;
        _configuration = configuration;
        _logHelper = logHelper;
    }

    public async Task<AccountingPaymentReminderRunResponse> PreviewAsync(int? daysBefore = null)
    {
        var options = ResolveOptions(daysBefore);
        var tasks = await _repository.CreateTasksAsync(
            options.DaysBefore,
            options.AccountingRoleId,
            options.CardStatusId,
            options.CreatedUserId,
            previewOnly: true);

        return BuildResponse(tasks, options.DaysBefore, created: false);
    }

    public async Task<AccountingPaymentReminderRunResponse> RunAsync(int? daysBefore = null)
    {
        var options = ResolveOptions(daysBefore);

        try
        {
            var tasks = await _repository.CreateTasksAsync(
                options.DaysBefore,
                options.AccountingRoleId,
                options.CardStatusId,
                options.CreatedUserId,
                previewOnly: false);

            return BuildResponse(tasks, options.DaysBefore, created: true);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(AccountingPaymentReminderService), ex);
            throw;
        }
    }

    private AccountingPaymentReminderOptions ResolveOptions(int? daysBefore)
    {
        var section = _configuration.GetSection("AccountingPaymentReminder");
        return new AccountingPaymentReminderOptions
        {
            DaysBefore = daysBefore ?? RequireInt(section, "DaysBefore"),
            AccountingRoleId = RequireInt(section, "AccountingRoleId"),
            CardStatusId = RequireInt(section, "CardStatusId"),
            CreatedUserId = RequireInt(section, "CreatedUserId")
        };
    }

    private static int RequireInt(IConfiguration section, string key) =>
        section.GetValue<int?>(key)
        ?? throw new InvalidOperationException($"AccountingPaymentReminder:{key} is not configured.");

    private static AccountingPaymentReminderRunResponse BuildResponse(
        List<AccountingPaymentReminderTaskResult> tasks,
        int daysBefore,
        bool created)
    {
        var fromDate = DateTime.Today;
        var toDate = DateTime.Today.AddDays(daysBefore);
        var count = tasks.Count;
        var verb = created ? "Created" : "Found";

        return new AccountingPaymentReminderRunResponse
        {
            CreatedCount = created ? count : 0,
            FromDate = fromDate,
            ToDate = toDate,
            Message = count == 0
                ? $"No student fees due between {fromDate:dd-MMM-yyyy} and {toDate:dd-MMM-yyyy}."
                : $"{verb} {count} accounting payment reminder task(s) for fees due {fromDate:dd-MMM-yyyy} to {toDate:dd-MMM-yyyy}.",
            Tasks = tasks
        };
    }

    private sealed class AccountingPaymentReminderOptions
    {
        public int DaysBefore { get; set; }
        public int AccountingRoleId { get; set; }
        public int CardStatusId { get; set; }
        public int CreatedUserId { get; set; }
    }
}
