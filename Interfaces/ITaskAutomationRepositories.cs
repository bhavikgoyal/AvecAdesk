using AvecADeskApi.Model.Reminder;

namespace AvecADeskApi.Interfaces;

public interface IContractExpiryReminderRepository
{
    Task<List<ContractExpiryTaskResult>> CreateTasksAsync(
        int daysBefore,
        string assignedUserName,
        string cardStatusName,
        int createdUserId,
        bool previewOnly);
}

public interface IInvoiceDueReminderRepository
{
    Task<List<InvoiceDueTaskResult>> CreateTasksAsync(
        string assignedUserName,
        string cardStatusName,
        string doneStatusName,
        int createdUserId,
        bool previewOnly);
}
