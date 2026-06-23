using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.College;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Colleges;

public class CollegeRepository : ICollegeRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public CollegeRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<CollegeFilterOptionsResponse> GetFilterOptionsAsync()
    {
        try
        {
            var campuses = await _db.ExecuteReaderListAsync(
                "sp_GetCollegeCampuses",
                _ => { },
                r => r.GetString(r.GetOrdinal("Campus")));

            var states = await _db.ExecuteReaderListAsync(
                "sp_GetCollegeStates",
                _ => { },
                r => r.GetString(r.GetOrdinal("State")));

            var partnerCount = await _db.ExecuteScalarAsync("sp_GetCollegePartnerCount", _ => { });

            return new CollegeFilterOptionsResponse
            {
                Campuses = campuses,
                States = states,
                PartnerCount = partnerCount is int count ? count : Convert.ToInt32(partnerCount ?? 0),
            };
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CollegeRepository)}.{nameof(GetFilterOptionsAsync)}", ex);
            throw;
        }
    }

    public async Task<List<CollegeSummaryResponse>> SearchCollegesAsync(
        string? query,
        string? campus,
        string? state,
        int? topCount)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_SearchColleges",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Query", string.IsNullOrWhiteSpace(query) ? DBNull.Value : query.Trim());
                    cmd.Parameters.AddWithValue("@Campus", string.IsNullOrWhiteSpace(campus) ? DBNull.Value : campus.Trim());
                    cmd.Parameters.AddWithValue("@State", string.IsNullOrWhiteSpace(state) ? DBNull.Value : state.Trim());
                    cmd.Parameters.AddWithValue("@TopCount", topCount.HasValue ? topCount.Value : DBNull.Value);
                },
                MapCollege);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CollegeRepository)}.{nameof(SearchCollegesAsync)}", ex);
            throw;
        }
    }

    private static CollegeSummaryResponse MapCollege(SqlDataReader reader)
    {
        return new CollegeSummaryResponse
        {
            InstituteName = ReadString(reader, "InstituteName") ?? string.Empty,
            Logo = ReadString(reader, "Logo"),
            WebsiteURL = ReadString(reader, "WebsiteURL"),
            ProgramCount = reader.GetInt32(reader.GetOrdinal("ProgramCount")),
            CampusCount = reader.GetInt32(reader.GetOrdinal("CampusCount")),
            Cities = SplitPipeList(ReadString(reader, "Cities")),
            Campuses = SplitPipeList(ReadString(reader, "Campuses")),
            States = SplitPipeList(ReadString(reader, "States")),
        };
    }

    private static List<string> SplitPipeList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        return value
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ReadString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
