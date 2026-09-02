using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteContact;
using Microsoft.Data.SqlClient;

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

    public async Task<InstituteContactDto?> GetByInstituteIdAsync(int instituteId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_GetInstituteContact",
                cmd => cmd.Parameters.AddWithValue("@InstituteId", instituteId),
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContactRepository)}.{nameof(GetByInstituteIdAsync)}", ex);
            throw;
        }
    }

    public async Task UpsertAsync(int instituteId, InstituteContactUpsertRequest request)
    {
        try
        {
            await _db.ExecuteNonQueryAsync("sp_UpsertInstituteContact", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                cmd.Parameters.AddWithValue("@ContactName", (object?)request.ContactName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Designation", (object?)request.Designation ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)request.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", (object?)request.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AlternatePhone", (object?)request.AlternatePhone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object?)request.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);
            });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteContactRepository)}.{nameof(UpsertAsync)}", ex);
            throw;
        }
    }

    private static InstituteContactDto MapRow(SqlDataReader reader)
    {
        return new InstituteContactDto
        {
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