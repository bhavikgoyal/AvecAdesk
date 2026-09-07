using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteCredential;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.InstituteCredential;

public class InstituteCredentialRepository : IInstituteCredentialRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public InstituteCredentialRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<IEnumerable<InstituteCredentialDto>> GetByInstituteIdAsync(int instituteId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetInstituteCredentials",
                cmd => cmd.Parameters.AddWithValue("@InstituteId", instituteId),
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteCredentialRepository)}.{nameof(GetByInstituteIdAsync)}", ex);
            throw;
        }
    }

    public async Task<InstituteCredentialDto?> GetByIdAsync(int instituteId, int credentialId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_GetInstituteCredentialById",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                    cmd.Parameters.AddWithValue("@Id", credentialId);
                },
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteCredentialRepository)}.{nameof(GetByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateAsync(int instituteId, InstituteCredentialUpsertRequest request)
    {
        try
        {
            var idParam = new SqlParameter("@Id", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_CreateInstituteCredential", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                cmd.Parameters.AddWithValue("@Name", request.Name);
                cmd.Parameters.AddWithValue("@Url", request.Url);
                cmd.Parameters.AddWithValue("@Username", request.Username);
                cmd.Parameters.AddWithValue("@Password", request.Password);
                cmd.Parameters.Add(idParam);
            });
            return (int)idParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteCredentialRepository)}.{nameof(CreateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int instituteId, int credentialId, InstituteCredentialUpsertRequest request)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_UpdateInstituteCredential", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                cmd.Parameters.AddWithValue("@Id", credentialId);
                cmd.Parameters.AddWithValue("@Name", request.Name);
                cmd.Parameters.AddWithValue("@Url", request.Url);
                cmd.Parameters.AddWithValue("@Username", request.Username);
                cmd.Parameters.AddWithValue("@Password", request.Password);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteCredentialRepository)}.{nameof(UpdateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int instituteId, int credentialId)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_DeleteInstituteCredential", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                cmd.Parameters.AddWithValue("@Id", credentialId);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteCredentialRepository)}.{nameof(DeleteAsync)}", ex);
            throw;
        }
    }

    private static InstituteCredentialDto MapRow(SqlDataReader reader)
    {
        return new InstituteCredentialDto
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            InstituteId = reader.GetInt32(reader.GetOrdinal("InstituteId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Url = reader.GetString(reader.GetOrdinal("Url")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            Password = reader.GetString(reader.GetOrdinal("Password")),
        };
    }
}