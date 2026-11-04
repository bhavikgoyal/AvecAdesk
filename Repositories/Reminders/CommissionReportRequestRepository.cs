using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;
using System.Data;

namespace AvecADeskApi.Repositories.Reminders;

public class CommissionReportRequestRepository : ICommissionReportRequestRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public CommissionReportRequestRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<CommissionReportRequestRecipient>> GetRecipientsAsync(DateTime reportMonth)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "dbo.sp_GetCommissionReportRequestRecipients",
                cmd => cmd.Parameters.Add("@ReportMonth", SqlDbType.Date).Value = reportMonth.Date,
                reader => new CommissionReportRequestRecipient
                {
                    ScrappingId = reader.GetInt32(reader.GetOrdinal("ScrappingId")),
                    InstituteName = reader.IsDBNull(reader.GetOrdinal("InstituteName"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("InstituteName")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Email")),
                    AlreadySent = reader.GetBoolean(reader.GetOrdinal("AlreadySent")),
                });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionReportRequestRepository)}.{nameof(GetRecipientsAsync)}", ex);
            throw;
        }
    }

    public async Task LogAsync(DateTime reportMonth, CommissionReportRequestRecipient college, string status, string? errorMessage)
    {
        try
        {
            await _db.ExecuteNonQueryAsync("dbo.sp_LogCommissionReportRequest", cmd =>
            {
                cmd.Parameters.Add("@ReportMonth", SqlDbType.Date).Value = reportMonth.Date;
                cmd.Parameters.Add("@ScrappingId", SqlDbType.Int).Value = college.ScrappingId;
                cmd.Parameters.Add("@InstituteName", SqlDbType.NVarChar, 500).Value = college.InstituteName;
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = (object?)college.Email ?? DBNull.Value;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = status;
                cmd.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar, 1000).Value = (object?)errorMessage ?? DBNull.Value;
            });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionReportRequestRepository)}.{nameof(LogAsync)}", ex);
            throw;
        }
    }
}
