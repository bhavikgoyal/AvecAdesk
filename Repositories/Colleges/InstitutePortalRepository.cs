using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.College;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Colleges;

public class InstitutePortalRepository : IInstitutePortalRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public InstitutePortalRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<InstitutePortalResponse?> GetPortalByInstituteNameAsync(
        string instituteName,
        string? query,
        string? level,
        string? intake,
        string? campus)
    {
        if (string.IsNullOrWhiteSpace(instituteName))
            return null;

        try
        {
            var trimmedName = instituteName.Trim();

            var profile = await _db.ExecuteReaderSingleAsync(
                "sp_GetInstitutePortalProfile",
                cmd => cmd.Parameters.AddWithValue("@InstituteName", trimmedName),
                MapProfile);

            if (profile == null)
                return null;

            var courses = await _db.ExecuteReaderListAsync(
                "sp_GetInstitutePortalCourses",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@InstituteName", trimmedName);
                    cmd.Parameters.AddWithValue("@Query", string.IsNullOrWhiteSpace(query) ? DBNull.Value : query.Trim());
                    cmd.Parameters.AddWithValue("@Level", string.IsNullOrWhiteSpace(level) ? DBNull.Value : level.Trim());
                    cmd.Parameters.AddWithValue("@Intake", string.IsNullOrWhiteSpace(intake) ? DBNull.Value : intake.Trim());
                    cmd.Parameters.AddWithValue("@Campus", string.IsNullOrWhiteSpace(campus) ? DBNull.Value : campus.Trim());
                },
                MapCourse);

            return new InstitutePortalResponse
            {
                Profile = profile,
                Courses = courses,
            };
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstitutePortalRepository)}.{nameof(GetPortalByInstituteNameAsync)}", ex);
            throw;
        }
    }

    private static InstitutePortalProfileResponse MapProfile(SqlDataReader reader)
    {
        return new InstitutePortalProfileResponse
        {
            InstituteName = ReadString(reader, "InstituteName") ?? string.Empty,
            Logo = ReadString(reader, "Logo"),
            WebsiteURL = ReadString(reader, "WebsiteURL"),
            Description = ReadString(reader, "Description"),
            ScholarshipsDetails = ReadString(reader, "ScholarshipsDetails"),
            Country = ReadString(reader, "Country"),
            City = ReadString(reader, "City"),
            CountryRanking = ReadString(reader, "CountryRanking"),
            Name = ReadString(reader, "Name"),
        };
    }

    private static InstitutePortalCourseResponse MapCourse(SqlDataReader reader)
    {
        return new InstitutePortalCourseResponse
        {
            ScrappingId = reader.GetInt32(reader.GetOrdinal("ScrappingId")),
            InstituteName = ReadString(reader, "InstituteName") ?? string.Empty,
            Campus = ReadString(reader, "Campus"),
            State = ReadString(reader, "State"),
            ProgramName = ReadString(reader, "ProgramName"),
            Level = ReadString(reader, "Level"),
            ProgramLink = ReadString(reader, "ProgramLink"),
            CricosCode = ReadString(reader, "CricosCode"),
            Duration = ReadString(reader, "Duration"),
            Intake = ReadString(reader, "Intake"),
            FeesYearly = ReadString(reader, "FeesYearly"),
            EnglishReq = ReadString(reader, "EnglishReq"),
            Logo = ReadString(reader, "Logo"),
            Country = ReadString(reader, "Country"),
            City = ReadString(reader, "City"),
        };
    }

    private static string? ReadString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
