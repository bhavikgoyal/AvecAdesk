using AvecADeskApi.Model.Reminder;

namespace AvecADeskApi.Interfaces;

public interface IAccountingPaymentReminderRepository
{
    Task<List<AccountingPaymentReminderTaskResult>> CreateTasksAsync(
        int daysBefore,
        int accountingRoleId,
        int cardStatusId,
        int createdUserId,
        bool previewOnly);
}
