using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteContact;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.InstituteContact;

public class InstituteContactRepository : IInstituteContactRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public InstituteContactRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<IEnumerable<InstituteContactDto>> GetByInstituteIdAsync(int instituteId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetInstituteContacts",
                cmd => cmd.Parameters.AddWithValue("@InstituteId", instituteId),
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContactRepository)}.{nameof(GetByInstituteIdAsync)}", ex);
            throw;
        }
    }

    public async Task<InstituteContactDto?> GetByIdAsync(int instituteId, int contactId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_GetInstituteContactById",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                    cmd.Parameters.AddWithValue("@Id", contactId);
                },
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContactRepository)}.{nameof(GetByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateAsync(int instituteId, InstituteContactUpsertRequest request)
    {
        try
        {
            var idParam = new SqlParameter("@Id", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_CreateInstituteContact", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                AddUpsertParameters(cmd, request);
                cmd.Parameters.Add(idParam);
            });
            return (int)idParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContactRepository)}.{nameof(CreateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int instituteId, int contactId, InstituteContactUpsertRequest request)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_UpdateInstituteContact", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                cmd.Parameters.AddWithValue("@Id", contactId);
                AddUpsertParameters(cmd, request);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContactRepository)}.{nameof(UpdateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int instituteId, int contactId)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_DeleteInstituteContact", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                cmd.Parameters.AddWithValue("@Id", contactId);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContactRepository)}.{nameof(DeleteAsync)}", ex);
            throw;
        }
    }

    private static void AddUpsertParameters(SqlCommand cmd, InstituteContactUpsertRequest request)
    {
        cmd.Parameters.AddWithValue("@ContactName", (object?)request.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Designation", (object?)request.Designation ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Email", (object?)request.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Phone", (object?)request.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AlternatePhone", (object?)request.AlternatePhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Address", (object?)request.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);
    }

    private static InstituteContactDto MapRow(SqlDataReader reader)
    {
        return new InstituteContactDto
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            InstituteId = reader.GetInt32(reader.GetOrdinal("InstituteId")),
            ContactName = ReadString(reader, "ContactName"),
            Designation = ReadString(reader, "Designation"),
            Email = ReadString(reader, "Email"),
            Phone = ReadString(reader, "Phone"),
            AlternatePhone = ReadString(reader, "AlternatePhone"),
            Address = ReadString(reader, "Address"),
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
}