using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.PaymentSchedule;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace AvecADeskApi.Repositories.PaymentSchedules;

public class ScheduleRepository : IScheduleRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;
    private readonly string _connectionString;

    public ScheduleRepository(SqlDbHelper db, LogHelper logHelper, IConfiguration configuration)
    {
        _db = db;
        _logHelper = logHelper;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is missing.");
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
    public async Task<List<StudentPaymentScheduleListResponse>> GetStudentPaymentScheduleListAsync(int? studentId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetStudentPaymentScheduleList",
                cmd => cmd.Parameters.AddWithValue("@StudentId", (object?)studentId ?? DBNull.Value),
                MapStudentPaymentSchedule);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(GetStudentPaymentScheduleListAsync)}", ex);
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
            var scheduleIdParam = new SqlParameter("@ScheduleId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_CreatePaymentSchedule", cmd =>
            {
                cmd.Parameters.AddWithValue("@StudentId", request.StudentId);
                cmd.Parameters.AddWithValue("@TotalCourseFee", request.TotalCourseFee);
                cmd.Parameters.AddWithValue("@NoOfInstallments", request.NoOfInstallments);
                cmd.Parameters.AddWithValue("@Frequency", request.Frequency);
                cmd.Parameters.AddWithValue("@FirstDueDate", request.FirstDueDate);

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
    public async Task<int> CreateStudentPaymentInstallmentAsync(StudentPaymentInstallmentCreateRequest request)
    {
        try
        {
            var installmentIdParam = new SqlParameter("@StudentPaymentInstallmentId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_CreateStudentPaymentInstallment", cmd =>
            {
                cmd.Parameters.AddWithValue("@ScheduleId", request.ScheduleId);
                cmd.Parameters.AddWithValue("@InstallmentNo", request.InstallmentNo);
                cmd.Parameters.AddWithValue("@DueDate", request.DueDate);
                cmd.Parameters.AddWithValue("@FeesAmount", request.FeesAmount);
                cmd.Parameters.AddWithValue("@PaidAmount", request.PaidAmount);
                cmd.Parameters.AddWithValue("@BalanceAmount", request.BalanceAmount);
                cmd.Parameters.AddWithValue("@PaymentStatus", request.PaymentStatus);

                cmd.Parameters.Add(installmentIdParam);
            });

            return (int)installmentIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(CreateStudentPaymentInstallmentAsync)}", ex);
            throw;
        }
    }
    public async Task<int> CreateStudentCommissionAsync(StudentCommissionCreateRequest request)
    {
        try
        {
            var commissionIdParam = new SqlParameter("@CommissionId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_CreateStudentCommission", cmd =>
            {
                cmd.Parameters.AddWithValue("@ScheduleId", request.ScheduleId);
                cmd.Parameters.AddWithValue("@CommissionPercentage", request.CommissionPercentage);
                cmd.Parameters.AddWithValue("@GSTPercentage", request.GSTPercentage);
                cmd.Parameters.AddWithValue("@BonusType", (object?)request.BonusType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BonusOption", (object?)request.BonusOption ?? DBNull.Value);

                cmd.Parameters.Add(commissionIdParam);
            });

            return (int)commissionIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(CreateStudentCommissionAsync)}", ex);
            throw;
        }
    }
    public async Task<int> CreateStudentCommissionDetailAsync(StudentCommissionDetailCreateRequest request)
    {
        try
        {
            var commissionDetailIdParam = new SqlParameter("@CommissionDetailId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_CreateStudentCommissionDetail", cmd =>
            {
                cmd.Parameters.AddWithValue("@CommissionId", request.CommissionId);
                cmd.Parameters.AddWithValue("@StudentPaymentInstallmentId", request.StudentPaymentInstallmentId);
                cmd.Parameters.AddWithValue("@CommissionAmount", request.CommissionAmount);
                cmd.Parameters.AddWithValue("@GSTAmount", request.GSTAmount);
                cmd.Parameters.AddWithValue("@BonusAmount", request.BonusAmount);
                cmd.Parameters.AddWithValue("@InvoiceAmount", request.InvoiceAmount);

                cmd.Parameters.AddWithValue("@InvoiceNo", (object?)request.InvoiceNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReceivedDate", (object?)request.ReceivedDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CommissionStatus", (object?)request.CommissionStatus ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remark", (object?)request.Remark ?? DBNull.Value);

                cmd.Parameters.Add(commissionDetailIdParam);
            });

            return (int)commissionDetailIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(CreateStudentCommissionDetailAsync)}", ex);
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

            var updated = (int)rowsAffectedParam.Value > 0;

            // sp_UpdatePaymentScheduleStatus may overwrite AmountPaid with AmountDue when status is Paid.
            if (updated && amountPaid.HasValue)
            {
                await PersistAmountPaidAsync(scheduleId, amountPaid.Value);
            }

            return updated;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(UpdatePaymentScheduleStatusAsync)}", ex);
            throw;
        }
    }

    private async Task PersistAmountPaidAsync(int scheduleId, decimal amountPaid)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "UPDATE PaymentSchedules SET AmountPaid = @AmountPaid WHERE ScheduleId = @ScheduleId",
            connection);
        command.Parameters.AddWithValue("@AmountPaid", amountPaid);
        command.Parameters.AddWithValue("@ScheduleId", scheduleId);
        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    //public async Task<int> BulkUpdatePaymentScheduleStatusAsync(PaymentScheduleBulkStatusRequest request)
    //{
    //    try
    //    {
    //        var updatedCount = 0;
    //        foreach (var item in request.Items)
    //        {
    //            var updated = await UpdatePaymentScheduleStatusAsync(item.ScheduleId, item.Status, item.AmountPaid);
    //            if (updated)
    //                updatedCount++;
    //        }

    //        return updatedCount;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(BulkUpdatePaymentScheduleStatusAsync)}", ex);
    //        throw;
    //    }
    //}

    public async Task<PaymentScheduleBulkStatusResult> BulkUpdatePaymentScheduleStatusAsync(
    PaymentScheduleBulkStatusRequest request)
    {
        try
        {
            var result = new PaymentScheduleBulkStatusResult();

            foreach (var item in request.Items)
            {
                try
                {
                    var updated = await UpdatePaymentScheduleStatusAsync(
                        item.ScheduleId,
                        item.Status,
                        item.AmountPaid);

                    if (updated)
                    {
                        result.UpdatedCount++;
                    }
                    else
                    {
                        result.FailedCount++;
                    }

                    result.Items.Add(new PaymentScheduleBulkItemResult
                    {
                        ScheduleId = item.ScheduleId,
                        Success = updated,
                        Error = updated ? null : "Schedule not found or update failed."
                    });
                }
                catch (Exception ex)
                {
                    result.FailedCount++;

                    result.Items.Add(new PaymentScheduleBulkItemResult
                    {
                        ScheduleId = item.ScheduleId,
                        Success = false,
                        Error = ex.Message
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logHelper.LogError(
                $"{nameof(ScheduleRepository)}.{nameof(BulkUpdatePaymentScheduleStatusAsync)}",
                ex);

            throw;
        }
    }

    //public async Task<PaymentScheduleSummaryResponse> GetPaymentSummaryAsync()
    //{
    //    try
    //    {
    //        var schedules = await GetPaymentSchedulesAsync(null);
    //        var today = DateTime.Today;

    //        static bool IsUnpaid(PaymentScheduleResponse schedule) =>
    //            !string.Equals(schedule.Status, "Paid", StringComparison.OrdinalIgnoreCase);

    //        return new PaymentScheduleSummaryResponse
    //        {
    //            CollectedTotal = schedules.Sum(schedule => schedule.AmountPaid),
    //            OutstandingTotal = schedules.Sum(schedule => schedule.AmountDue),
    //            OverdueTotal = schedules
    //                .Where(schedule => IsUnpaid(schedule) && schedule.DueDate.Date < today)
    //                .Sum(schedule => schedule.AmountDue),
    //            OverdueCount = schedules.Count(schedule => IsUnpaid(schedule) && schedule.DueDate.Date < today),
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logHelper.LogError($"{nameof(ScheduleRepository)}.{nameof(GetPaymentSummaryAsync)}", ex);
    //        throw;
    //    }
    //}

    private static PaymentScheduleResponse MapSchedule(SqlDataReader reader)
    {
        return new PaymentScheduleResponse
        {
            ScheduleId = reader.GetInt32(reader.GetOrdinal("ScheduleId")),
            StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
            FirstDueDate = reader.GetDateTime(reader.GetOrdinal("FirstDueDate")),
            TotalCourseFee = reader.IsDBNull(reader.GetOrdinal("TotalCourseFee"))
                ? null
                : reader.GetDecimal(reader.GetOrdinal("TotalCourseFee")),
            NoOfInstallments = reader.IsDBNull(reader.GetOrdinal("NoOfInstallments"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("NoOfInstallments")),
            Frequency = reader.IsDBNull(reader.GetOrdinal("Frequency"))
                ? null
                : reader.GetString(reader.GetOrdinal("Frequency")),
            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
        };
    }
    private static StudentPaymentScheduleListResponse MapStudentPaymentSchedule(SqlDataReader reader)
    {
        return new StudentPaymentScheduleListResponse
        {
            ScheduleId = reader.GetInt32(reader.GetOrdinal("ScheduleId")),
            StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
            StudentName = reader["StudentName"]?.ToString() ?? "",
            InstituteName = reader["InstituteName"]?.ToString() ?? "",
            CourseName = reader["CourseName"]?.ToString() ?? "",
            TotalCourseFee = reader.GetDecimal(reader.GetOrdinal("TotalCourseFee")),
            NoOfInstallments = reader.GetInt32(reader.GetOrdinal("NoOfInstallments")),
            Frequency = reader["Frequency"]?.ToString() ?? "",
            FirstDueDate = reader.GetDateTime(reader.GetOrdinal("FirstDueDate")),
            TotalInstallments = reader.GetInt32(reader.GetOrdinal("TotalInstallments")),
            PaidInstallments = reader.GetInt32(reader.GetOrdinal("PaidInstallments")),
            PendingInstallments = reader.GetInt32(reader.GetOrdinal("PendingInstallments")),
            CollectedAmount = reader.GetDecimal(reader.GetOrdinal("CollectedAmount")),
            BalanceAmount = reader.GetDecimal(reader.GetOrdinal("BalanceAmount")),
            NextDueDate = reader.IsDBNull(reader.GetOrdinal("NextDueDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("NextDueDate")),
            PaymentStatus = reader["PaymentStatus"]?.ToString() ?? ""
        };
    }
    public async Task<UpdateStudentPaymentScheduleResponse> UpdateStudentPaymentScheduleAsync(UpdateStudentPaymentScheduleRequest request)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("sp_UpdateStudentPaymentSchedule", connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@StudentId", request.StudentId);
            command.Parameters.AddWithValue("@NoOfInstallments", request.NoOfInstallments);
            command.Parameters.AddWithValue("@Frequency", request.Frequency);
            command.Parameters.AddWithValue("@FirstDueDate", request.FirstDueDate);

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new UpdateStudentPaymentScheduleResponse
                {
                    ScheduleId = reader.GetInt32(reader.GetOrdinal("ScheduleId")),
                    CommissionId = reader.GetInt32(reader.GetOrdinal("CommissionId"))
                };
            }

            throw new Exception("Payment schedule update failed.");
        }
        catch (Exception ex)
        {
            _logHelper.LogError(
                $"{nameof(ScheduleRepository)}.{nameof(UpdateStudentPaymentScheduleAsync)}",
                ex);

            throw;
        }
    }
}
