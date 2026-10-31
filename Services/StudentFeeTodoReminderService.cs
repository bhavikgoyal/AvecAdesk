using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;

namespace AvecADeskApi.Services;

public class StudentFeeTodoReminderService
{
    private const string SectionName = "StudentFeeTodoReminder";

    private readonly IStudentFeeTodoReminderRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;

    public StudentFeeTodoReminderService(
        IStudentFeeTodoReminderRepository repository,
        IConfiguration configuration,
        LogHelper logHelper)
    {
        _repository = repository;
        _configuration = configuration;
        _logHelper = logHelper;
    }

    public async Task<AccountingPaymentReminderRunResponse> RunAsync()
    {
        var section = _configuration.GetSection(SectionName);
        var daysBefore = RequireInt(section, "DaysBefore");

        try
        {
            var createdUserId = RequireInt(section, "CreatedUserId");
            var boardId = RequireInt(section, "BoardId");
            var listName = RequireString(section, "ListName");

            var accountingRoleId = RequireInt(section, "AccountingRoleId");
            var completedListName = RequireString(section, "CompletedListName");

            // Unticked items from earlier days move onto today's card before today's reminders are added.
            await _repository.ForwardOpenTodoItemsAsync(daysBefore, accountingRoleId, createdUserId, boardId, listName, completedListName);

            var upcoming = await _repository.CreateTasksAsync(
                daysBefore,
                accountingRoleId,
                createdUserId,
                boardId,
                listName,
                completedListName,
                previewOnly: false);

            var invoices = await _repository.CreateCommissionInvoiceTasksAsync(
                RequireInt(section, "InvoiceReminderDaysBefore"),
                accountingRoleId,
                createdUserId,
                boardId,
                listName,
                previewOnly: false);

            var overdue = await _repository.CreateOverdueTasksAsync(
                RequireString(section, "OverdueAssignRoles"),
                createdUserId,
                boardId,
                listName,
                previewOnly: false);

            var feeDate = DateTime.Today.AddDays(daysBefore);
            return new AccountingPaymentReminderRunResponse
            {
                CreatedCount = upcoming.Count + invoices.Count + overdue.Count,
                FromDate = feeDate,
                ToDate = feeDate,
                Message = $"Added {upcoming.Count} student fee reminder(s) for fees due on {feeDate:dd-MMM-yyyy}, "
                    + $"{invoices.Count} commission invoice reminder(s) and {overdue.Count} overdue fee reminder(s).",
                Tasks = [.. upcoming, .. invoices, .. overdue]
            };
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(StudentFeeTodoReminderService), ex);
            throw;
        }
    }

    private static int RequireInt(IConfiguration section, string key) =>
        section.GetValue<int?>(key)
        ?? throw new InvalidOperationException($"{SectionName}:{key} is not configured.");

    private static string RequireString(IConfiguration section, string key) =>
        section.GetValue<string>(key) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"{SectionName}:{key} is not configured.");
}
