using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;

namespace AvecADeskApi.Services;

/// <summary>
/// Keeps an open Task/Card for the Admin for every invoice that is not
/// yet Paid, and automatically moves the card to "Done" once the
/// invoice's status becomes Paid.
/// </summary>
public class InvoiceDueReminderService
{
    private readonly IInvoiceDueReminderRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;

    public InvoiceDueReminderService(
        IInvoiceDueReminderRepository repository,
        IConfiguration configuration,
        LogHelper logHelper)
    {
        _repository = repository;
        _configuration = configuration;
        _logHelper = logHelper;
    }

    public Task<InvoiceDueTaskRunResponse> PreviewAsync() => RunInternalAsync(previewOnly: true);

    public Task<InvoiceDueTaskRunResponse> RunAsync() => RunInternalAsync(previewOnly: false);

    private async Task<InvoiceDueTaskRunResponse> RunInternalAsync(bool previewOnly)
    {
        var assignedUserName = _configuration["InvoiceDueReminder:AssignedUserName"] ?? "shivalimehendale2";
        var cardStatusName = _configuration["InvoiceDueReminder:CardStatusName"] ?? "Payment Collection";
        var doneStatusName = _configuration["InvoiceDueReminder:DoneStatusName"] ?? "Done list";
        var createdUserId = _configuration.GetValue<int>("InvoiceDueReminder:CreatedUserId");

        try
        {
            var tasks = await _repository.CreateTasksAsync(assignedUserName, cardStatusName, doneStatusName, createdUserId, previewOnly);

            var createdCount = tasks.Count(t => t.Action is "WillCreate" or "Created");
            var doneCount = tasks.Count(t => t.Action is "WillMarkDone" or "MarkedDone");

            return new InvoiceDueTaskRunResponse
            {
                CreatedCount = createdCount,
                MarkedDoneCount = doneCount,
                Message = previewOnly
                    ? $"{createdCount} unpaid invoice(s) need a follow-up task; {doneCount} can be marked done."
                    : $"Created {createdCount} follow-up task(s); marked {doneCount} as done (invoice now Paid).",
                Tasks = tasks
            };
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceDueReminderService)}.{nameof(RunInternalAsync)}", ex);
            throw;
        }
    }
}
