using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;

namespace AvecADeskApi.Services;

/// <summary>
/// Creates a Task/Card for the Admin (e.g. Shivali) whenever an institute or
/// student contract is within N days of expiring, so it can be renewed.
/// </summary>
public class ContractExpiryReminderService
{
    private readonly IContractExpiryReminderRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;

    public ContractExpiryReminderService(
        IContractExpiryReminderRepository repository,
        IConfiguration configuration,
        LogHelper logHelper)
    {
        _repository = repository;
        _configuration = configuration;
        _logHelper = logHelper;
    }

    public Task<ContractExpiryTaskRunResponse> PreviewAsync(int? daysBefore = null)
        => RunInternalAsync(daysBefore, previewOnly: true);

    public Task<ContractExpiryTaskRunResponse> RunAsync(int? daysBefore = null)
        => RunInternalAsync(daysBefore, previewOnly: false);

    private async Task<ContractExpiryTaskRunResponse> RunInternalAsync(int? daysBefore, bool previewOnly)
    {
        var days = daysBefore ?? _configuration.GetValue("ContractExpiryReminder:DaysBefore", 10);
        var assignedUserName = _configuration["ContractExpiryReminder:AssignedUserName"] ?? "shivalimehendale2";
        var cardStatusName = _configuration["ContractExpiryReminder:CardStatusName"] ?? "CONTRACT EXPIRY";
        var createdUserId = _configuration.GetValue<int>("ContractExpiryReminder:CreatedUserId");

        try
        {
            var tasks = await _repository.CreateTasksAsync(days, assignedUserName, cardStatusName, createdUserId, previewOnly);

            var createdCount = previewOnly
                ? tasks.Count(t => t.Action == "WillCreate")
                : tasks.Count(t => t.Action == "Created");

            return new ContractExpiryTaskRunResponse
            {
                CreatedCount = createdCount,
                DaysBefore = days,
                Message = previewOnly
                    ? $"{createdCount} contract(s) expiring within {days} day(s) need a renewal task."
                    : $"Created {createdCount} renewal task(s) for contracts expiring within {days} day(s).",
                Tasks = tasks
            };
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ContractExpiryReminderService)}.{nameof(RunInternalAsync)}", ex);
            throw;
        }
    }
}
