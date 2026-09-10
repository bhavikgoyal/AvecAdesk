using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Reminders;

public class InvoiceDueReminderRepository : IInvoiceDueReminderRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public InvoiceDueReminderRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<InvoiceDueTaskResult>> CreateTasksAsync(
        string assignedUserName,
        string cardStatusName,
        string doneStatusName,
        int createdUserId,
        bool previewOnly)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "dbo.sp_CreateInvoiceDueReminderTasks",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@AssignedUserName", assignedUserName);
                    cmd.Parameters.AddWithValue("@CardStatusName", cardStatusName);
                    cmd.Parameters.AddWithValue("@DoneStatusName", doneStatusName);
                    cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                    cmd.Parameters.AddWithValue("@PreviewOnly", previewOnly);
                },
                MapResult);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceDueReminderRepository)}.{nameof(CreateTasksAsync)}", ex);
            throw;
        }
    }

    private static InvoiceDueTaskResult MapResult(SqlDataReader reader) => new()
    {
        CardID = reader["CardID"] is DBNull ? null : reader.GetInt32(reader.GetOrdinal("CardID")),
        InvoiceId = reader.GetInt32(reader.GetOrdinal("InvoiceId")),
        CardTitle = reader["CardTitle"] is DBNull ? null : reader.GetString(reader.GetOrdinal("CardTitle")),
        AssignedUserID = reader.GetInt32(reader.GetOrdinal("AssignedUserID")),
        Action = reader["Action"] as string ?? string.Empty
    };
}
