using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Reminders;

public class ContractExpiryReminderRepository : IContractExpiryReminderRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public ContractExpiryReminderRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<ContractExpiryTaskResult>> CreateTasksAsync(
        int daysBefore,
        string assignedUserName,
        string cardStatusName,
        int createdUserId,
        bool previewOnly)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "dbo.sp_CreateContractExpiryReminderTasks",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@DaysBefore", daysBefore);
                    cmd.Parameters.AddWithValue("@AssignedUserName", assignedUserName);
                    cmd.Parameters.AddWithValue("@CardStatusName", cardStatusName);
                    cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                    cmd.Parameters.AddWithValue("@PreviewOnly", previewOnly);
                },
                MapResult);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ContractExpiryReminderRepository)}.{nameof(CreateTasksAsync)}", ex);
            throw;
        }
    }

    private static ContractExpiryTaskResult MapResult(SqlDataReader reader) => new()
    {
        SourceType = reader["SourceType"] as string ?? string.Empty,
        SourceReferenceId = reader.GetInt32(reader.GetOrdinal("SourceReferenceId")),
        CardTitle = reader["CardTitle"] as string ?? string.Empty,
        DueDate = reader["DueDate"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
        CardID = reader["CardID"] is DBNull ? null : reader.GetInt32(reader.GetOrdinal("CardID")),
        AssignedUserID = reader.GetInt32(reader.GetOrdinal("AssignedUserID")),
        Action = reader["Action"] as string ?? string.Empty
    };
}
