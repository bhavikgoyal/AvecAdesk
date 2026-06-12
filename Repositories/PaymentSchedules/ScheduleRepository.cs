using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.PaymentSchedule;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.PaymentSchedules;

public class ScheduleRepository : IScheduleRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public ScheduleRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<PaymentScheduleResponse>> GetPaymentSchedulesAsync(int? studentId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetPaymentSchedules",
                cmd => cmd.Parameters.AddWithValue("@StudentId", (object?)studentId ?? DBNull.Value),
                MapSchedule);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(GetPaymentSchedulesAsync)}", ex);
            throw;
        }
    }

    public async Task<PaymentScheduleResponse?> GetPaymentScheduleByIdAsync(int scheduleId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_GetPaymentScheduleById",
                cmd => cmd.Parameters.AddWithValue("@ScheduleId", scheduleId),
                MapSchedule);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(GetPaymentScheduleByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreatePaymentScheduleAsync(PaymentScheduleCreateRequest request)
    {
        try
        {
            var scheduleIdParam = new SqlParameter("@ScheduleId", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.ExecuteNonQueryAsync("sp_CreatePaymentSchedule", cmd =>
            {
                cmd.Parameters.AddWithValue("@StudentId", request.StudentId);
                cmd.Parameters.AddWithValue("@DueDate", request.DueDate);
                cmd.Parameters.AddWithValue("@AmountDue", request.AmountDue);
                cmd.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);
                cmd.Parameters.Add(scheduleIdParam);
            });

            return (int)scheduleIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(CreatePaymentScheduleAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdatePaymentScheduleAsync(int scheduleId, PaymentScheduleUpdateRequest request)
    {
        try
        {
            var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.ExecuteNonQueryAsync("sp_UpdatePaymentSchedule", cmd =>
            {
                cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                cmd.Parameters.AddWithValue("@StudentId", request.StudentId);
                cmd.Parameters.AddWithValue("@DueDate", request.DueDate);
                cmd.Parameters.AddWithValue("@AmountDue", request.AmountDue);
                cmd.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);
                cmd.Parameters.Add(rowsAffectedParam);
            });

            return (int)rowsAffectedParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(UpdatePaymentScheduleAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdatePaymentScheduleStatusAsync(int scheduleId, string status, decimal? amountPaid)
    {
        try
        {
            var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.ExecuteNonQueryAsync("sp_UpdatePaymentScheduleStatus", cmd =>
            {
                cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@AmountPaid", (object?)amountPaid ?? DBNull.Value);
                cmd.Parameters.Add(rowsAffectedParam);
            });

            return (int)rowsAffectedParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(UpdatePaymentScheduleStatusAsync)}", ex);
            throw;
        }
    }

    public async Task<int> BulkUpdatePaymentScheduleStatusAsync(PaymentScheduleBulkStatusRequest request)
    {
        try
        {
            var updatedCount = 0;
            foreach (var item in request.Items)
            {
                var updated = await UpdatePaymentScheduleStatusAsync(item.ScheduleId, item.Status, item.AmountPaid);
                if (updated)
                    updatedCount++;
            }

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(BulkUpdatePaymentScheduleStatusAsync)}", ex);
            throw;
        }
    }

    private static PaymentScheduleResponse MapSchedule(SqlDataReader reader)
    {
        return new PaymentScheduleResponse
        {
            ScheduleId = reader.GetInt32(reader.GetOrdinal("ScheduleId")),
            StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
            DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
            AmountDue = reader.GetDecimal(reader.GetOrdinal("AmountDue")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            AmountPaid = reader.GetDecimal(reader.GetOrdinal("AmountPaid")),
            PaidAt = reader.IsDBNull(reader.GetOrdinal("PaidAt")) ? null : reader.GetDateTime(reader.GetOrdinal("PaidAt")),
            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes"))
        };
    }
}
