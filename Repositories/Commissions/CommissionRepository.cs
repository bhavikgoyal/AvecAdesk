using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Commission;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Commissions;

public class CommissionRepository : ICommissionRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public CommissionRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<CommissionRateResponse>> GetVendorCommissionRatesAsync(int vendorId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetVendorCommissionRates",
                cmd => cmd.Parameters.AddWithValue("@VendorId", vendorId),
                MapRate);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionRepository)}.{nameof(GetVendorCommissionRatesAsync)}", ex);
            throw;
        }
    }

    public async Task<int> SetVendorCommissionRateAsync(int vendorId, CommissionRateCreateRequest request)
    {
        try
        {
            var commissionIdParam = new SqlParameter("@CommissionId", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.ExecuteNonQueryAsync("sp_SetVendorCommissionRate", cmd =>
            {
                cmd.Parameters.AddWithValue("@VendorId", vendorId);
                cmd.Parameters.AddWithValue("@InstituteId", (object?)request.InstituteId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CourseId", (object?)request.CourseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RateType", request.RateType);
                cmd.Parameters.AddWithValue("@Rate", request.Rate);
                cmd.Parameters.AddWithValue("@EffectiveFrom", request.EffectiveFrom);
                cmd.Parameters.AddWithValue("@EffectiveTo", (object?)request.EffectiveTo ?? DBNull.Value);
                cmd.Parameters.Add(commissionIdParam);
            });

            return (int)commissionIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionRepository)}.{nameof(SetVendorCommissionRateAsync)}", ex);
            throw;
        }
    }

    public async Task<List<CommissionRateResponse>> GetInstituteCommissionRatesAsync(int instituteId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetInstituteCommissionRates",
                cmd => cmd.Parameters.AddWithValue("@InstituteId", instituteId),
                MapRate);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionRepository)}.{nameof(GetInstituteCommissionRatesAsync)}", ex);
            throw;
        }
    }

    public async Task<int> SetInstituteCommissionRateAsync(int instituteId, CommissionRateCreateRequest request)
    {
        try
        {
            var commissionIdParam = new SqlParameter("@CommissionId", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.ExecuteNonQueryAsync("sp_SetInstituteCommissionRate", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                cmd.Parameters.AddWithValue("@CourseId", (object?)request.CourseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RateType", request.RateType);
                cmd.Parameters.AddWithValue("@Rate", request.Rate);
                cmd.Parameters.AddWithValue("@EffectiveFrom", request.EffectiveFrom);
                cmd.Parameters.AddWithValue("@EffectiveTo", (object?)request.EffectiveTo ?? DBNull.Value);
                cmd.Parameters.Add(commissionIdParam);
            });

            return (int)commissionIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionRepository)}.{nameof(SetInstituteCommissionRateAsync)}", ex);
            throw;
        }
    }

    public async Task<List<CommissionEarningResponse>> GetCommissionEarningsAsync(int? vendorId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetCommissionEarnings",
                cmd => cmd.Parameters.AddWithValue("@VendorId", (object?)vendorId ?? DBNull.Value),
                MapEarning);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionRepository)}.{nameof(GetCommissionEarningsAsync)}", ex);
            throw;
        }
    }

    public async Task<CommissionForecastResponse?> GetCommissionEarningsForecastAsync(int? vendorId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_GetCommissionEarningsForecast",
                cmd => cmd.Parameters.AddWithValue("@VendorId", (object?)vendorId ?? DBNull.Value),
                MapForecast);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionRepository)}.{nameof(GetCommissionEarningsForecastAsync)}", ex);
            throw;
        }
    }

    public async Task<CommissionEarningResponse?> ApproveCommissionEarningAsync(int earningId, int? approvedByUserId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_ApproveCommissionEarning",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@EarningId", earningId);
                    cmd.Parameters.AddWithValue("@ApprovedByUserId", (object?)approvedByUserId ?? DBNull.Value);
                },
                MapEarning);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionRepository)}.{nameof(ApproveCommissionEarningAsync)}", ex);
            throw;
        }
    }

    public async Task<List<CommissionEarningResponse>> GetCommissionStatementAsync(int vendorId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetCommissionStatement",
                cmd => cmd.Parameters.AddWithValue("@VendorId", vendorId),
                MapEarning);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CommissionRepository)}.{nameof(GetCommissionStatementAsync)}", ex);
            throw;
        }
    }

    private static CommissionRateResponse MapRate(SqlDataReader reader)
    {
        return new CommissionRateResponse
        {
            CommissionId = reader.GetInt32(reader.GetOrdinal("CommissionId")),
            VendorId = reader.GetInt32(reader.GetOrdinal("VendorId")),
            InstituteId = reader.IsDBNull(reader.GetOrdinal("InstituteId")) ? null : reader.GetInt32(reader.GetOrdinal("InstituteId")),
            CourseId = reader.IsDBNull(reader.GetOrdinal("CourseId")) ? null : reader.GetInt32(reader.GetOrdinal("CourseId")),
            RateType = reader.GetString(reader.GetOrdinal("RateType")),
            Rate = reader.GetDecimal(reader.GetOrdinal("Rate")),
            EffectiveFrom = reader.GetDateTime(reader.GetOrdinal("EffectiveFrom")),
            EffectiveTo = reader.IsDBNull(reader.GetOrdinal("EffectiveTo")) ? null : reader.GetDateTime(reader.GetOrdinal("EffectiveTo"))
        };
    }

    private static CommissionEarningResponse MapEarning(SqlDataReader reader)
    {
        return new CommissionEarningResponse
        {
            EarningId = reader.GetInt32(reader.GetOrdinal("EarningId")),
            VendorId = reader.GetInt32(reader.GetOrdinal("VendorId")),
            CommissionId = reader.GetInt32(reader.GetOrdinal("CommissionId")),
            StudentPaymentId = reader.GetInt32(reader.GetOrdinal("StudentPaymentId")),
            EarnedAmount = reader.GetDecimal(reader.GetOrdinal("EarnedAmount")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            ApprovedByUserId = reader.IsDBNull(reader.GetOrdinal("ApprovedByUserId")) ? null : reader.GetInt32(reader.GetOrdinal("ApprovedByUserId")),
            ApprovedAt = reader.IsDBNull(reader.GetOrdinal("ApprovedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ApprovedAt"))
        };
    }

    private static CommissionForecastResponse MapForecast(SqlDataReader reader)
    {
        return new CommissionForecastResponse
        {
            VendorId = reader.IsDBNull(reader.GetOrdinal("VendorId")) ? 0 : reader.GetInt32(reader.GetOrdinal("VendorId")),
            TotalPending = reader.GetDecimal(reader.GetOrdinal("TotalPending")),
            TotalApproved = reader.GetDecimal(reader.GetOrdinal("TotalApproved")),
            TotalPaid = reader.GetDecimal(reader.GetOrdinal("TotalPaid")),
            AnticipatedAmount = reader.GetDecimal(reader.GetOrdinal("AnticipatedAmount")),
            RecordCount = reader.GetInt32(reader.GetOrdinal("RecordCount"))
        };
    }
}
