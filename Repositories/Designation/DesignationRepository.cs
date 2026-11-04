using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Designation;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Designation;

public class DesignationRepository : IDesignationRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public DesignationRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<IEnumerable<DesignationDto>> GetAllAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetDesignations",
                _ => { },
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(DesignationRepository)}.{nameof(GetAllAsync)}", ex);
            throw;
        }
    }

    private static DesignationDto MapRow(SqlDataReader reader)
    {
        return new DesignationDto
        {
            DesignationId = reader.GetInt32(reader.GetOrdinal("DesignationId")),
            DesignationName = reader.GetString(reader.GetOrdinal("DesignationName")),
        };
    }
}
