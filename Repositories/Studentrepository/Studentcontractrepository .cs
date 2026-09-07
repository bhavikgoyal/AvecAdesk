using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;
using System.Data;
using StudentContractModel = AvecADeskApi.Model.StudentContract.StudentContract;
using StudentContractUpsertRequest = AvecADeskApi.Model.StudentContract.StudentContractUpsertRequest;

namespace AvecADeskApi.Repositories.StudentContract;

public class StudentContractRepository : IStudentContractRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public StudentContractRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<IEnumerable<StudentContractModel>> GetByStudentIdAsync(int studentId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetStudentContracts",
                cmd => cmd.Parameters.AddWithValue("@StudentId", studentId),
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentContractRepository)}.{nameof(GetByStudentIdAsync)}", ex);
            throw;
        }
    }

    public async Task<StudentContractModel?> GetByIdAsync(int studentId, int contractId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_GetStudentContractById",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@StudentId", studentId);
                    cmd.Parameters.AddWithValue("@Id", contractId);
                },
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentContractRepository)}.{nameof(GetByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateAsync(int studentId, StudentContractUpsertRequest request)
    {
        try
        {
            var idParam = new SqlParameter("@Id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync(
                "sp_CreateStudentContract",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@StudentId", studentId);
                    AddUpsertParameters(cmd, request);
                    cmd.Parameters.Add(idParam);
                });

            if (idParam.Value == null || idParam.Value == DBNull.Value)
                throw new InvalidOperationException("Failed to retrieve contract ID.");

            return Convert.ToInt32(idParam.Value);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentContractRepository)}.{nameof(CreateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int studentId, int contractId, StudentContractUpsertRequest request)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync(
                "sp_UpdateStudentContract",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@StudentId", studentId);
                    cmd.Parameters.AddWithValue("@Id", contractId);
                    AddUpsertParameters(cmd, request);
                    cmd.Parameters.Add(rowsParam);
                });

            return rowsParam.Value != null && rowsParam.Value != DBNull.Value && Convert.ToInt32(rowsParam.Value) > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentContractRepository)}.{nameof(UpdateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int studentId, int contractId)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync(
                "sp_DeleteStudentContract",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@StudentId", studentId);
                    cmd.Parameters.AddWithValue("@Id", contractId);
                    cmd.Parameters.Add(rowsParam);
                });

            return rowsParam.Value != null && rowsParam.Value != DBNull.Value && Convert.ToInt32(rowsParam.Value) > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentContractRepository)}.{nameof(DeleteAsync)}", ex);
            throw;
        }
    }

    private static void AddUpsertParameters(SqlCommand cmd, StudentContractUpsertRequest request)
    {
        cmd.Parameters.Add("@ContractStatus", SqlDbType.NVarChar, 50).Value =
            (object?)request.ContractStatus ?? "Active";

        cmd.Parameters.Add("@ContractStartDate", SqlDbType.Date).Value =
            (object?)request.ContractStartDate ?? DBNull.Value;

        cmd.Parameters.Add("@ContractEndDate", SqlDbType.Date).Value =
            (object?)request.ContractEndDate ?? DBNull.Value;

        cmd.Parameters.Add("@ContractReferenceNo", SqlDbType.NVarChar, 100).Value =
            (object?)request.ContractReferenceNo ?? DBNull.Value;

        cmd.Parameters.Add("@ContractFileUrl", SqlDbType.NVarChar, 500).Value =
            (object?)request.ContractFileUrl ?? DBNull.Value;

        cmd.Parameters.Add("@ContractFileName", SqlDbType.NVarChar, 255).Value =
            (object?)request.ContractFileName ?? DBNull.Value;

        cmd.Parameters.Add("@Notes", SqlDbType.NVarChar, -1).Value =
            (object?)request.Notes ?? DBNull.Value;
    }

    private static StudentContractModel MapRow(SqlDataReader reader)
    {
        return new StudentContractModel
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
            ContractStatus = ReadString(reader, "ContractStatus") ?? "Active",
            ContractStartDate = ReadDate(reader, "ContractStartDate"),
            ContractEndDate = ReadDate(reader, "ContractEndDate"),
            ContractReferenceNo = ReadString(reader, "ContractReferenceNo"),
            ContractFileUrl = ReadString(reader, "ContractFileUrl"),
            ContractFileName = ReadString(reader, "ContractFileName"),
            Notes = ReadString(reader, "Notes")
        };
    }

    private static int GetOrdinalSafe(SqlDataReader reader, string column)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(column, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static string? ReadString(SqlDataReader reader, string column)
    {
        var ordinal = GetOrdinalSafe(reader, column);
        return (ordinal == -1 || reader.IsDBNull(ordinal)) ? null : reader.GetString(ordinal);
    }

    private static DateTime? ReadDate(SqlDataReader reader, string column)
    {
        var ordinal = GetOrdinalSafe(reader, column);
        return (ordinal == -1 || reader.IsDBNull(ordinal)) ? null : reader.GetDateTime(ordinal);
    }
}