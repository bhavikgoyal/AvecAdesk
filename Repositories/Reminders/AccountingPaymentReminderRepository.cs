using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Reminders;

public class AccountingPaymentReminderRepository : IAccountingPaymentReminderRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public AccountingPaymentReminderRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<AccountingPaymentReminderTaskResult>> CreateTasksAsync(
        int daysBefore,
        int accountingRoleId,
        int cardStatusId,
        int createdUserId,
        bool previewOnly)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "dbo.sp_CreateAccountingPaymentReminderTasks",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@DaysBefore", daysBefore);
                    cmd.Parameters.AddWithValue("@AccountingRoleId", accountingRoleId);
                    cmd.Parameters.AddWithValue("@CardStatusID", cardStatusId);
                    cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                    cmd.Parameters.AddWithValue("@PreviewOnly", previewOnly);
                },
                MapResult);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AccountingPaymentReminderRepository)}.{nameof(CreateTasksAsync)}", ex);
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
