using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.College;
using AvecADeskApi.Model.Course;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace AvecADeskApi.Repositories.Colleges;

public class InstitutePortalRepository : IInstitutePortalRepository
{
    private readonly SqlDbHelper _db;
    private readonly ICourseRepository _courseRepository;
    private readonly LogHelper _logHelper;

    public InstitutePortalRepository(
        SqlDbHelper db,
        ICourseRepository courseRepository,
        LogHelper logHelper)
    {
        _db = db;
        _courseRepository = courseRepository;
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

            // INSTITUTEScrapping.ScrappingId == Courses.InstituteId
            // Example: Notre Dame ScrappingId=2 → all Courses where InstituteId=2
            var instituteId = await ResolveInstituteIdAsync(trimmedName);
            var courses = new List<InstitutePortalCourseResponse>();

            if (instituteId is > 0)
            {
                // Load from Courses table by InstituteId (all programs for this institute)
                var allCourses = await _courseRepository.GetCoursesAsync();
                courses = allCourses
                    .Where(c => c.InstituteId == instituteId.Value && c.IsActive)
                    .Where(c => MatchesCourseListFilters(c, query, level, intake, campus))
                    .Select(c => MapFromCourseList(c, trimmedName, profile))
                    .OrderBy(c => c.ProgramName)
                    .ToList();
            }

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

    private async Task<int?> ResolveInstituteIdAsync(string instituteName)
    {
        var scrapRows = await _db.ExecuteReaderListAsync(
            "sp_GetInstituteScrapping",
            _ => { },
            reader => new InstituteIdHolder
            {
                Id = reader.GetInt32(reader.GetOrdinal("ScrappingId")),
                Name = ReadString(reader, "InstituteName"),
            });

        var match = scrapRows.FirstOrDefault(r =>
            string.Equals(r.Name?.Trim(), instituteName, StringComparison.OrdinalIgnoreCase));

        return match?.Id > 0 ? match.Id : null;
    }

    private static bool MatchesCourseListFilters(
        CourseListResponse course,
        string? query,
        string? level,
        string? intake,
        string? campus)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            var haystack = $"{course.CourseName} {course.Level} {course.Campus} {course.CourseCategory}";
            if (haystack.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(level)
            && !string.Equals(course.Level?.Trim(), level.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(intake)
            && (course.Intake?.IndexOf(intake.Trim(), StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
            return false;

        if (!string.IsNullOrWhiteSpace(campus)
            && (course.Campus?.IndexOf(campus.Trim(), StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
            return false;

        return true;
    }

    private static InstitutePortalCourseResponse MapFromCourseList(
        CourseListResponse course,
        string instituteName,
        InstitutePortalProfileResponse profile)
    {
        return new InstitutePortalCourseResponse
        {
            CourseId = course.CourseId,
            InstituteId = course.InstituteId,
            ScrappingId = course.CourseId,
            InstituteName = string.IsNullOrWhiteSpace(course.InstituteName) ? instituteName : course.InstituteName,
            Campus = course.Campus,
            State = null,
            ProgramName = course.CourseName,
            Level = course.Level,
            ProgramLink = course.ProgramLink,
            CricosCode = null,
            Duration = course.Duration,
            Intake = course.Intake,
            FeesYearly = course.Fees?.ToString("0.##", CultureInfo.InvariantCulture),
            EnglishReq = null,
            ProgramLogo = course.ProgramLogo,
            Logo = profile.Logo,
            Country = profile.Country,
            City = profile.City,
            Description = null,
        };
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

    private static string? ReadString(SqlDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    private sealed class InstituteIdHolder
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
