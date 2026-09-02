using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteContract;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.InstituteContract;

public class InstituteContractRepository : IInstituteContractRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public InstituteContractRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<IEnumerable<InstituteContractDto>> GetByInstituteIdAsync(int instituteId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetInstituteContracts",
                cmd => cmd.Parameters.AddWithValue("@InstituteId", instituteId),
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContractRepository)}.{nameof(GetByInstituteIdAsync)}", ex);
            throw;
        }
    }

    public async Task<InstituteContractDto?> GetByIdAsync(int instituteId, int contractId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_GetInstituteContractById",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                    cmd.Parameters.AddWithValue("@Id", contractId);
                },
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContractRepository)}.{nameof(GetByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateAsync(int instituteId, InstituteContractUpsertRequest request)
    {
        try
        {
            var idParam = new SqlParameter("@Id", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_CreateInstituteContract", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                AddUpsertParameters(cmd, request);
                cmd.Parameters.Add(idParam);
            });
            return (int)idParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContractRepository)}.{nameof(CreateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int instituteId, int contractId, InstituteContractUpsertRequest request)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_UpdateInstituteContract", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                cmd.Parameters.AddWithValue("@Id", contractId);
                AddUpsertParameters(cmd, request);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContractRepository)}.{nameof(UpdateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int instituteId, int contractId)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_DeleteInstituteContract", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                cmd.Parameters.AddWithValue("@Id", contractId);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContractRepository)}.{nameof(DeleteAsync)}", ex);
            throw;
        }
    }

    private static void AddUpsertParameters(SqlCommand cmd, InstituteContractUpsertRequest request)
    {
        cmd.Parameters.AddWithValue("@ContractStatus", request.ContractStatus ?? "Active");
        cmd.Parameters.AddWithValue("@ContractStartDate", (object?)request.ContractStartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContractEndDate", (object?)request.ContractEndDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContractReferenceNo", (object?)request.ContractReferenceNo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContractFileUrl", (object?)request.ContractFileUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);
    }

    private static InstituteContractDto MapRow(SqlDataReader reader)
    {
        return new InstituteContractDto
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            InstituteId = reader.GetInt32(reader.GetOrdinal("InstituteId")),
            ContractStatus = ReadString(reader, "ContractStatus") ?? "Active",
            ContractStartDate = ReadDate(reader, "ContractStartDate"),
            ContractEndDate = ReadDate(reader, "ContractEndDate"),
            ContractReferenceNo = ReadString(reader, "ContractReferenceNo"),
            ContractFileUrl = ReadString(reader, "ContractFileUrl"),
            Notes = ReadString(reader, "Notes"),
        };
    }

    private static string? ReadString(SqlDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch (IndexOutOfRangeException) { return null; }
    }

    private static DateTime? ReadDate(SqlDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
        catch (IndexOutOfRangeException) { return null; }
    }
}