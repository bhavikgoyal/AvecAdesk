using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Upload;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Uploads;

public class UploadRepository : IUploadRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public UploadRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<InstituteUploadResponse>> GetUploadsAsync(int? instituteId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync("sp_GetInstituteUploads",
                cmd => cmd.Parameters.AddWithValue("@InstituteId", (object?)instituteId ?? DBNull.Value),
                MapUpload);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(UploadRepository)}.{nameof(GetUploadsAsync)}", ex);
            throw;
        }
    }

    public async Task<InstituteUploadResponse?> GetUploadByIdAsync(int uploadId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync("sp_GetInstituteUploadById",
                cmd => cmd.Parameters.AddWithValue("@UploadId", uploadId), MapUpload);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(UploadRepository)}.{nameof(GetUploadByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> UploadInstituteExcelAsync(int instituteId, int uploadedByUserId, string filePath)
    {
        try
        {
            var uploadIdParam = new SqlParameter("@UploadId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_UploadInstituteExcel", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                cmd.Parameters.AddWithValue("@UploadedByUserId", uploadedByUserId);
                cmd.Parameters.AddWithValue("@FilePath", filePath);
                cmd.Parameters.Add(uploadIdParam);
            });
            return (int)uploadIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(UploadRepository)}.{nameof(UploadInstituteExcelAsync)}", ex);
            throw;
        }
    }

    public async Task<UploadDiffResponse?> GetUploadDiffAsync(int uploadId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync("sp_GetInstituteUploadDiff",
                cmd => cmd.Parameters.AddWithValue("@UploadId", uploadId), MapDiff);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(UploadRepository)}.{nameof(GetUploadDiffAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> ReconcileUploadAsync(int uploadId)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_ReconcileInstituteUpload", cmd =>
            {
                cmd.Parameters.AddWithValue("@UploadId", uploadId);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(UploadRepository)}.{nameof(ReconcileUploadAsync)}", ex);
            throw;
        }
    }

    private static InstituteUploadResponse MapUpload(SqlDataReader r) => new()
    {
        UploadId = r.GetInt32(r.GetOrdinal("UploadId")),
        InstituteId = r.GetInt32(r.GetOrdinal("InstituteId")),
        UploadedByUserId = r.GetInt32(r.GetOrdinal("UploadedByUserId")),
        FilePath = r.GetString(r.GetOrdinal("FilePath")),
        UploadedAt = r.GetDateTime(r.GetOrdinal("UploadedAt")),
        ParseStatus = r.GetString(r.GetOrdinal("ParseStatus")),
        ChangesSummary = r.IsDBNull(r.GetOrdinal("ChangesSummary")) ? null : r.GetString(r.GetOrdinal("ChangesSummary")),
        DiscrepancyCount = r.GetInt32(r.GetOrdinal("DiscrepancyCount"))
    };

    private static UploadDiffResponse MapDiff(SqlDataReader r) => new()
    {
        UploadId = r.GetInt32(r.GetOrdinal("UploadId")),
        InstituteId = r.GetInt32(r.GetOrdinal("InstituteId")),
        ChangesSummary = r.IsDBNull(r.GetOrdinal("ChangesSummary")) ? null : r.GetString(r.GetOrdinal("ChangesSummary")),
        DiscrepancyCount = r.GetInt32(r.GetOrdinal("DiscrepancyCount")),
        ParseStatus = r.GetString(r.GetOrdinal("ParseStatus"))
    };
}
