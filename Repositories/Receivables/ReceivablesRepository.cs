using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Receivables;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Receivables;

public class ReceivablesRepository : IReceivablesRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public ReceivablesRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<AnticipatedReceivableResponse>> GetAnticipatedAsync(ReceivablesFilter filter)
    {
        try
        {
            return await _db.ExecuteReaderListAsync("sp_GetAnticipatedReceivables",
                cmd => AddFilterParams(cmd, filter), MapAnticipated);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ReceivablesRepository)}.{nameof(GetAnticipatedAsync)}", ex);
            throw;
        }
    }

    public async Task<List<OverdueReceivableResponse>> GetOverdueAsync(ReceivablesFilter filter)
    {
        try
        {
            return await _db.ExecuteReaderListAsync("sp_GetOverdueReceivables",
                cmd => AddFilterParams(cmd, filter), MapOverdue);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ReceivablesRepository)}.{nameof(GetOverdueAsync)}", ex);
            throw;
        }
    }

    public async Task<List<ReceivedPaymentResponse>> GetReceivedAsync(ReceivablesFilter filter)
    {
        try
        {
            return await _db.ExecuteReaderListAsync("sp_GetReceivedPayments",
                cmd => AddFilterParams(cmd, filter), MapReceived);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ReceivablesRepository)}.{nameof(GetReceivedAsync)}", ex);
            throw;
        }
    }

    public async Task<ReceivablesSummaryResponse> GetSummaryAsync(ReceivablesFilter filter)
    {
        try
        {
            var result = await _db.ExecuteReaderSingleAsync("sp_GetReceivablesSummary",
                cmd => AddFilterParams(cmd, filter), MapSummary);
            return result ?? new ReceivablesSummaryResponse();
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ReceivablesRepository)}.{nameof(GetSummaryAsync)}", ex);
            throw;
        }
    }

    // ---- shared param binding -------------------------------------------------
    private static void AddFilterParams(SqlCommand cmd, ReceivablesFilter filter)
    {
        cmd.Parameters.AddWithValue("@FromDate", (object?)filter.FromDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ToDate", (object?)filter.ToDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InstituteId", (object?)filter.InstituteId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StudentId", (object?)filter.StudentId ?? DBNull.Value);
    }

    // ---- row mappers ------------------------------------------------------------
    private static AnticipatedReceivableResponse MapAnticipated(SqlDataReader r) => new()
    {
        ScheduleId = r.GetInt32(r.GetOrdinal("ScheduleId")),
        StudentId = r.GetInt32(r.GetOrdinal("StudentId")),
        StudentName = r.GetString(r.GetOrdinal("StudentName")),
        InstituteId = r.GetInt32(r.GetOrdinal("InstituteId")),
        InstituteName = r.GetString(r.GetOrdinal("InstituteName")),
        DueDate = r.GetDateTime(r.GetOrdinal("DueDate")),
        AmountDue = r.GetDecimal(r.GetOrdinal("AmountDue")),
        AmountPaid = r.GetDecimal(r.GetOrdinal("AmountPaid")),
        BalanceDue = r.GetDecimal(r.GetOrdinal("BalanceDue")),
        Status = r.GetString(r.GetOrdinal("Status")),
        Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? null : r.GetString(r.GetOrdinal("Notes"))
    };

    private static OverdueReceivableResponse MapOverdue(SqlDataReader r) => new()
    {
        ScheduleId = r.GetInt32(r.GetOrdinal("ScheduleId")),
        StudentId = r.GetInt32(r.GetOrdinal("StudentId")),
        StudentName = r.GetString(r.GetOrdinal("StudentName")),
        InstituteId = r.GetInt32(r.GetOrdinal("InstituteId")),
        InstituteName = r.GetString(r.GetOrdinal("InstituteName")),
        DueDate = r.GetDateTime(r.GetOrdinal("DueDate")),
        DaysOverdue = r.GetInt32(r.GetOrdinal("DaysOverdue")),
        AgingBucket = r.GetString(r.GetOrdinal("AgingBucket")),
        AmountDue = r.GetDecimal(r.GetOrdinal("AmountDue")),
        AmountPaid = r.GetDecimal(r.GetOrdinal("AmountPaid")),
        BalanceDue = r.GetDecimal(r.GetOrdinal("BalanceDue")),
        Status = r.GetString(r.GetOrdinal("Status")),
        Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? null : r.GetString(r.GetOrdinal("Notes"))
    };

    private static ReceivedPaymentResponse MapReceived(SqlDataReader r) => new()
    {
        ScheduleId = r.GetInt32(r.GetOrdinal("ScheduleId")),
        StudentId = r.GetInt32(r.GetOrdinal("StudentId")),
        StudentName = r.GetString(r.GetOrdinal("StudentName")),
        InstituteId = r.GetInt32(r.GetOrdinal("InstituteId")),
        InstituteName = r.GetString(r.GetOrdinal("InstituteName")),
        DueDate = r.GetDateTime(r.GetOrdinal("DueDate")),
        PaidAt = r.IsDBNull(r.GetOrdinal("PaidAt")) ? null : r.GetDateTime(r.GetOrdinal("PaidAt")),
        AmountDue = r.GetDecimal(r.GetOrdinal("AmountDue")),
        AmountPaid = r.GetDecimal(r.GetOrdinal("AmountPaid")),
        BalanceDue = r.GetDecimal(r.GetOrdinal("BalanceDue")),
        Status = r.GetString(r.GetOrdinal("Status")),
        Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? null : r.GetString(r.GetOrdinal("Notes"))
    };

    private static ReceivablesSummaryResponse MapSummary(SqlDataReader r) => new()
    {
        TotalAnticipated = r.GetDecimal(r.GetOrdinal("TotalAnticipated")),
        AnticipatedCount = r.GetInt32(r.GetOrdinal("AnticipatedCount")),
        TotalOverdue = r.GetDecimal(r.GetOrdinal("TotalOverdue")),
        OverdueCount = r.GetInt32(r.GetOrdinal("OverdueCount")),
        TotalReceived = r.GetDecimal(r.GetOrdinal("TotalReceived")),
        ReceivedCount = r.GetInt32(r.GetOrdinal("ReceivedCount"))
    };
}