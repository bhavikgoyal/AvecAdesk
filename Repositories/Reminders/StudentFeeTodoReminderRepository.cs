using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Reminders;

public class StudentFeeTodoReminderRepository : IStudentFeeTodoReminderRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public StudentFeeTodoReminderRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<AccountingPaymentReminderTaskResult>> CreateTasksAsync(int daysBefore,int accountingRoleId,int createdUserId,int boardId,
        string listName,string completedListName,bool previewOnly)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "dbo.sp_CreateStudentFeeTodoReminderTasks",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@DaysBefore", daysBefore);
                    cmd.Parameters.AddWithValue("@AccountingRoleId", accountingRoleId);
                    cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                    cmd.Parameters.AddWithValue("@BoardID", boardId);
                    cmd.Parameters.AddWithValue("@ListName", listName);
                    cmd.Parameters.AddWithValue("@CompletedListName", completedListName);
                    cmd.Parameters.AddWithValue("@PreviewOnly", previewOnly);
                },
                MapResult);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentFeeTodoReminderRepository)}.{nameof(CreateTasksAsync)}", ex);
            throw;
        }
    }

    public async Task<List<AccountingPaymentReminderTaskResult>> CreateOverdueTasksAsync(string assignRoleNames,int createdUserId,int boardId,string listName,bool previewOnly)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "dbo.sp_CreateStudentFeeOverdueTasks",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@AssignRoleNames", assignRoleNames);
                    cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                    cmd.Parameters.AddWithValue("@BoardID", boardId);
                    cmd.Parameters.AddWithValue("@ListName", listName);
                    cmd.Parameters.AddWithValue("@PreviewOnly", previewOnly);
                },
                MapResult);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentFeeTodoReminderRepository)}.{nameof(CreateOverdueTasksAsync)}", ex);
            throw;
        }
    }

    public async Task<int> ForwardOpenTodoItemsAsync(
        int daysBefore,
        int accountingRoleId,
        int createdUserId,
        int boardId,
        string listName,
        string completedListName)
    {
        try
        {
            var result = await _db.ExecuteScalarAsync("dbo.sp_ForwardStudentFeeTodoItems", cmd =>
            {
                cmd.Parameters.AddWithValue("@DaysBefore", daysBefore);
                cmd.Parameters.AddWithValue("@AccountingRoleId", accountingRoleId);
                cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                cmd.Parameters.AddWithValue("@BoardID", boardId);
                cmd.Parameters.AddWithValue("@ListName", listName);
                cmd.Parameters.AddWithValue("@CompletedListName", completedListName);
            });

            return result is null or DBNull ? 0 : Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentFeeTodoReminderRepository)}.{nameof(ForwardOpenTodoItemsAsync)}", ex);
            throw;
        }
    }

    public async Task<List<DuesTodoItemResponse>> GetDuesTodoItemsAsync(int daysBefore, string scope)
    {
        try
        {
            return await _db.ExecuteReaderListAsync("dbo.sp_GetDuesTodoItems",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@DaysBefore", daysBefore);
                    cmd.Parameters.AddWithValue("@Scope", scope);
                },
                reader => new DuesTodoItemResponse
                {
                    ItemName = reader["ItemName"] as string ?? string.Empty,
                    CardDate = reader["CardDate"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("CardDate")),
                    DueDate = reader["DueDate"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
                    IsOverdue = reader["IsOverdue"] is not DBNull && reader.GetBoolean(reader.GetOrdinal("IsOverdue")),
                    DaysLeft = reader["DaysLeft"] is DBNull ? 0 : reader.GetInt32(reader.GetOrdinal("DaysLeft")),
                    PendingUsers = reader.GetInt32(reader.GetOrdinal("PendingUsers")),
                    AssignedTo = reader["AssignedTo"] as string ?? string.Empty
                });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentFeeTodoReminderRepository)}.{nameof(GetDuesTodoItemsAsync)}", ex);
            throw;
        }
    }

    public async Task DismissDuesTodoItemAsync(string itemName, DateTime originalCardDate, int? dismissedByUserId)
    {
        try
        {
            await _db.ExecuteScalarAsync("dbo.sp_DismissDuesTodoItem", cmd =>
            {
                cmd.Parameters.AddWithValue("@ItemName", itemName);
                cmd.Parameters.AddWithValue("@OriginalCardDate", originalCardDate.Date);
                cmd.Parameters.AddWithValue("@DismissedByUserId", (object?)dismissedByUserId ?? DBNull.Value);
            });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentFeeTodoReminderRepository)}.{nameof(DismissDuesTodoItemAsync)}", ex);
            throw;
        }
    }

    public async Task<List<AccountingPaymentReminderTaskResult>> CreateCommissionInvoiceTasksAsync(
        int daysBeforeDue,
        int accountingRoleId,
        int createdUserId,
        int boardId,
        string listName,
        bool previewOnly)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "dbo.sp_CreateCommissionInvoiceReminderTasks",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@DaysBeforeDue", daysBeforeDue);
                    cmd.Parameters.AddWithValue("@AccountingRoleId", accountingRoleId);
                    cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                    cmd.Parameters.AddWithValue("@BoardID", boardId);
                    cmd.Parameters.AddWithValue("@ListName", listName);
                    cmd.Parameters.AddWithValue("@PreviewOnly", previewOnly);
                },
                MapResult);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentFeeTodoReminderRepository)}.{nameof(CreateCommissionInvoiceTasksAsync)}", ex);
            throw;
        }
    }

    private static AccountingPaymentReminderTaskResult MapResult(SqlDataReader reader) => new()
    {
        StudentPaymentInstallmentId = reader.GetInt32(reader.GetOrdinal("StudentPaymentInstallmentId")),
        CardID = reader["CardID"] is DBNull ? null : reader.GetInt32(reader.GetOrdinal("CardID")),
        StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
        StudentName = reader["StudentName"] as string ?? string.Empty,
        DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
        AmountDue = reader.GetDecimal(reader.GetOrdinal("AmountDue")),
        CardTitle = reader["CardTitle"] as string ?? string.Empty,
        AssignedUserID = reader["AssignedUserID"] is DBNull ? null : reader.GetInt32(reader.GetOrdinal("AssignedUserID")),
        AssignedTo = reader["AssignedTo"] as string ?? string.Empty
    };
}
