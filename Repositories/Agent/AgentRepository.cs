using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Agent;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Agent;

public class AgentRepository : IAgentRepository
{
    private const int DuplicateNameError = 50001;
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public AgentRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<IEnumerable<AgentDto>> GetAllAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync("dbo.sp_GetAgents", _ => { }, MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgentRepository)}.{nameof(GetAllAsync)}", ex);
            throw;
        }
    }

    public async Task<IEnumerable<AgentLookupDto>> GetActiveNamesAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "dbo.sp_GetActiveAgentNames",
                _ => { },
                reader => new AgentLookupDto
                {
                    AgentId = reader.GetInt32(reader.GetOrdinal("AgentId")),
                    AgentName = reader.GetString(reader.GetOrdinal("AgentName")),
                });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgentRepository)}.{nameof(GetActiveNamesAsync)}", ex);
            throw;
        }
    }

    public async Task<AgentDto?> GetByIdAsync(int agentId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "dbo.sp_GetAgentById",
                cmd => cmd.Parameters.Add("@AgentId", SqlDbType.Int).Value = agentId,
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgentRepository)}.{nameof(GetByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateAsync(AgentUpsertRequest request, int? userId)
    {
        try
        {
            var idParam = new SqlParameter("@AgentId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("dbo.sp_CreateAgent", cmd =>
            {
                AddUpsertParameters(cmd, request, userId);
                cmd.Parameters.Add(idParam);
            });
            return (int)idParam.Value;
        }
        catch (SqlException ex) when (ex.Number == DuplicateNameError)
        {
            throw new InvalidOperationException(ex.Message);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgentRepository)}.{nameof(CreateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int agentId, AgentUpsertRequest request, int? userId)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("dbo.sp_UpdateAgent", cmd =>
            {
                cmd.Parameters.Add("@AgentId", SqlDbType.Int).Value = agentId;
                AddUpsertParameters(cmd, request, userId);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (SqlException ex) when (ex.Number == DuplicateNameError)
        {
            throw new InvalidOperationException(ex.Message);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgentRepository)}.{nameof(UpdateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int agentId, int? userId)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("dbo.sp_DeleteAgent", cmd =>
            {
                cmd.Parameters.Add("@AgentId", SqlDbType.Int).Value = agentId;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = (object?)userId ?? DBNull.Value;
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgentRepository)}.{nameof(DeleteAsync)}", ex);
            throw;
        }
    }

    private static void AddUpsertParameters(SqlCommand cmd, AgentUpsertRequest request, int? userId)
    {
        cmd.Parameters.Add("@AgentName", SqlDbType.NVarChar, 200).Value = request.AgentName!.Trim();
        cmd.Parameters.Add("@ContactPerson", SqlDbType.NVarChar, 200).Value = ToDbValue(request.ContactPerson);
        cmd.Parameters.Add("@PhoneNo", SqlDbType.NVarChar, 20).Value = ToDbValue(request.PhoneNo);
        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = ToDbValue(request.Email);
        cmd.Parameters.Add("@Address", SqlDbType.NVarChar, 1000).Value = ToDbValue(request.Address);
        cmd.Parameters.Add("@Notes", SqlDbType.NVarChar, -1).Value = ToDbValue(request.Notes);
        cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = request.IsActive;
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = (object?)userId ?? DBNull.Value;
    }

    private static object ToDbValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static AgentDto MapRow(SqlDataReader reader)
    {
        var updatedOrdinal = reader.GetOrdinal("UpdatedOn");
        return new AgentDto
        {
            AgentId = reader.GetInt32(reader.GetOrdinal("AgentId")),
            AgentName = reader.GetString(reader.GetOrdinal("AgentName")),
            ContactPerson = ReadString(reader, "ContactPerson"),
            PhoneNo = ReadString(reader, "PhoneNo"),
            Email = ReadString(reader, "Email"),
            Address = ReadString(reader, "Address"),
            Notes = ReadString(reader, "Notes"),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedOn = reader.IsDBNull(updatedOrdinal) ? null : reader.GetDateTime(updatedOrdinal),
        };
    }

    private static string? ReadString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
