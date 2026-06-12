using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.Institute;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Institutes;

public class InstituteRepository : IInstituteRepository
{
    private readonly SqlDbHelper _db;

    public InstituteRepository(SqlDbHelper db)
    {
        _db = db;
    }

    public Task<List<InstituteResponse>> SearchInstitutesAsync(string? name, string? city, string? service)
    {
        return _db.ExecuteReaderListAsync(
            "sp_SearchInstitutes",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Name", (object?)name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@City", (object?)city ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Service", (object?)service ?? DBNull.Value);
            },
            MapInstitute);
    }

    public Task<InstituteResponse?> GetInstituteByIdAsync(int instituteId)
    {
        return _db.ExecuteReaderSingleAsync(
            "sp_GetInstituteById",
            cmd => cmd.Parameters.AddWithValue("@InstituteId", instituteId),
            MapInstitute);
    }

    public Task<List<InstituteResponse>> GetInstitutesAdminAsync(string? status)
    {
        return _db.ExecuteReaderListAsync(
            "sp_GetInstitutesAdmin",
            cmd => cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value),
            MapInstitute);
    }

    public async Task<int> CreateInstituteAsync(InstituteCreateRequest request)
    {
        var instituteIdParam = new SqlParameter("@InstituteId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_CreateInstitute", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@InstituteName", request.InstituteName);
            cmd.Parameters.AddWithValue("@WebsiteUrl", (object?)request.WebsiteUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LogoUrl", (object?)request.LogoUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PrimaryColour", (object?)request.PrimaryColour ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SecondaryColour", (object?)request.SecondaryColour ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object?)request.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@City", (object?)request.City ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@State", (object?)request.State ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ServiceTypes", (object?)request.ServiceTypes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactEmail", (object?)request.ContactEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactPhone", (object?)request.ContactPhone ?? DBNull.Value);
            cmd.Parameters.Add(instituteIdParam);
        });

        return (int)instituteIdParam.Value;
    }

    public async Task<bool> UpdateInstituteAsync(int instituteId, InstituteUpdateRequest request)
    {
        var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_UpdateInstitute", cmd =>
        {
            cmd.Parameters.AddWithValue("@InstituteId", instituteId);
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@InstituteName", request.InstituteName);
            cmd.Parameters.AddWithValue("@WebsiteUrl", (object?)request.WebsiteUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LogoUrl", (object?)request.LogoUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PrimaryColour", (object?)request.PrimaryColour ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SecondaryColour", (object?)request.SecondaryColour ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object?)request.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@City", (object?)request.City ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@State", (object?)request.State ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ServiceTypes", (object?)request.ServiceTypes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactEmail", (object?)request.ContactEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactPhone", (object?)request.ContactPhone ?? DBNull.Value);
            cmd.Parameters.Add(rowsAffectedParam);
        });

        return (int)rowsAffectedParam.Value > 0;
    }

    public async Task<bool> UpdateInstituteStatusAsync(int instituteId, string status)
    {
        var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_UpdateInstituteStatus", cmd =>
        {
            cmd.Parameters.AddWithValue("@InstituteId", instituteId);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.Add(rowsAffectedParam);
        });

        return (int)rowsAffectedParam.Value > 0;
    }

    public async Task<bool> PublishInstituteAsync(int instituteId)
    {
        var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_PublishInstitute", cmd =>
        {
            cmd.Parameters.AddWithValue("@InstituteId", instituteId);
            cmd.Parameters.Add(rowsAffectedParam);
        });

        return (int)rowsAffectedParam.Value > 0;
    }

    private static InstituteResponse MapInstitute(SqlDataReader reader)
    {
        return new InstituteResponse
        {
            InstituteId = reader.GetInt32(reader.GetOrdinal("InstituteId")),
            VendorId = reader.GetInt32(reader.GetOrdinal("VendorId")),
            InstituteName = reader.GetString(reader.GetOrdinal("InstituteName")),
            WebsiteUrl = reader.IsDBNull(reader.GetOrdinal("WebsiteUrl")) ? null : reader.GetString(reader.GetOrdinal("WebsiteUrl")),
            LogoUrl = reader.IsDBNull(reader.GetOrdinal("LogoUrl")) ? null : reader.GetString(reader.GetOrdinal("LogoUrl")),
            PrimaryColour = reader.IsDBNull(reader.GetOrdinal("PrimaryColour")) ? null : reader.GetString(reader.GetOrdinal("PrimaryColour")),
            SecondaryColour = reader.IsDBNull(reader.GetOrdinal("SecondaryColour")) ? null : reader.GetString(reader.GetOrdinal("SecondaryColour")),
            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
            City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City")),
            State = reader.IsDBNull(reader.GetOrdinal("State")) ? null : reader.GetString(reader.GetOrdinal("State")),
            ServiceTypes = reader.IsDBNull(reader.GetOrdinal("ServiceTypes")) ? null : reader.GetString(reader.GetOrdinal("ServiceTypes")),
            ContactEmail = reader.IsDBNull(reader.GetOrdinal("ContactEmail")) ? null : reader.GetString(reader.GetOrdinal("ContactEmail")),
            ContactPhone = reader.IsDBNull(reader.GetOrdinal("ContactPhone")) ? null : reader.GetString(reader.GetOrdinal("ContactPhone")),
            IsPublished = reader.GetBoolean(reader.GetOrdinal("IsPublished")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            LastScrapedAt = reader.IsDBNull(reader.GetOrdinal("LastScrapedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastScrapedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
