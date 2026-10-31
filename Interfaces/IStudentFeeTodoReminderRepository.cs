using AvecADeskApi.Model.Reminder;

namespace AvecADeskApi.Interfaces;

public interface IStudentFeeTodoReminderRepository
{
    Task<List<AccountingPaymentReminderTaskResult>> CreateTasksAsync(
        int daysBefore,
        int accountingRoleId,
        int createdUserId,
        int boardId,
        string listName,
        string completedListName,
        bool previewOnly);

    Task<int> ForwardOpenTodoItemsAsync(
        int daysBefore,
        int accountingRoleId,
        int createdUserId,
        int boardId,
        string listName,
        string completedListName);

    Task<List<DuesTodoItemResponse>> GetDuesTodoItemsAsync(int daysBefore, string scope);

    Task DismissDuesTodoItemAsync(string itemName, DateTime originalCardDate, int? dismissedByUserId);

    Task<List<AccountingPaymentReminderTaskResult>> CreateCommissionInvoiceTasksAsync(
        int daysBeforeDue,
        int accountingRoleId,
        int createdUserId,
        int boardId,
        string listName,
        bool previewOnly);

    Task<List<AccountingPaymentReminderTaskResult>> CreateOverdueTasksAsync(
        string assignRoleNames,
        int createdUserId,
        int boardId,
        string listName,
        bool previewOnly);
}
