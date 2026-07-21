using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.College;
using AvecADeskApi.Model.Course;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Colleges;

public class CollegeRepository : ICollegeRepository
{
    private readonly SqlDbHelper _db;
    private readonly ICourseRepository _courseRepository;
    private readonly LogHelper _logHelper;

    public CollegeRepository(
        SqlDbHelper db,
        ICourseRepository courseRepository,
        LogHelper logHelper)
    {
        _db = db;
        _courseRepository = courseRepository;
        _logHelper = logHelper;
    }

    public async Task<CollegeFilterOptionsResponse> GetFilterOptionsAsync()
    {
        try
        {
            var courses = await _courseRepository.GetCoursesAsync();
            var activeCourses = courses.Where(c => c.IsActive).ToList();

            var campuses = activeCourses
                .Select(c => c.Campus?.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .Cast<string>()
                .ToList();

            // State still comes from institute scrap profile (Courses table has no State column)
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
            // 1) Institute cards from INSTITUTEScrapping (logo / name / location)
            var institutes = await _db.ExecuteReaderListAsync(
                "sp_GetInstituteScrapping",
                _ => { },
                MapScrapInstitute);

            // One card per institute name (keep first ScrappingId = Courses.InstituteId)
            var uniqueInstitutes = institutes
                .Where(i => !string.IsNullOrWhiteSpace(i.InstituteName))
                .GroupBy(i => i.InstituteName!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderBy(x => x.ScrappingId).First())
                .ToList();

            // 2) All programs from Courses table
            var courses = (await _courseRepository.GetCoursesAsync())
                .Where(c => c.IsActive)
                .ToList();

            var result = new List<CollegeSummaryResponse>();

            foreach (var institute in uniqueInstitutes)
            {
                // Courses.InstituteId == INSTITUTEScrapping.ScrappingId
                var instituteCourses = courses
                    .Where(c => c.InstituteId == institute.ScrappingId)
                    .ToList();

                var courseCampuses = instituteCourses
                    .Select(c => c.Campus?.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Cast<string>()
                    .OrderBy(c => c)
                    .ToList();

                // If Courses.Campus is empty, fall back to INSTITUTEScrapping.Campus for display
                if (courseCampuses.Count == 0 && !string.IsNullOrWhiteSpace(institute.Campus))
                    courseCampuses = SplitValues(institute.Campus);

                // Location label: prefer scrap City/State; campus list from Courses (or scrap fallback)
                var cities = SplitValues(institute.City);
                var states = SplitValues(institute.State);
                if (cities.Count == 0)
                    cities = courseCampuses;

                var summary = new CollegeSummaryResponse
                {
                    InstituteName = institute.InstituteName!.Trim(),
                    Logo = institute.Logo,
                    WebsiteURL = institute.WebsiteURL,
                    ProgramCount = instituteCourses.Count,
                    CampusCount = courseCampuses.Count,
                    Campuses = courseCampuses,
                    Cities = cities,
                    States = states,
                };

                if (!MatchesSearch(summary, instituteCourses, query, campus, state))
                    continue;

                result.Add(summary);
            }

            result = result
                .OrderBy(c => c.InstituteName)
                .ToList();

            if (topCount is > 0)
                result = result.Take(topCount.Value).ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CollegeRepository)}.{nameof(SearchCollegesAsync)}", ex);
            throw;
        }
    }

    private static bool MatchesSearch(
        CollegeSummaryResponse college,
        List<CourseListResponse> instituteCourses,
        string? query,
        string? campus,
        string? state)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            var inName = college.InstituteName.Contains(q, StringComparison.OrdinalIgnoreCase);
            var inCourse = instituteCourses.Any(c =>
                (c.CourseName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (c.Campus?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
            if (!inName && !inCourse)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(campus))
        {
            var camp = campus.Trim();
            var hasCampus = college.Campuses.Any(c => c.Contains(camp, StringComparison.OrdinalIgnoreCase))
                || instituteCourses.Any(c => c.Campus?.Contains(camp, StringComparison.OrdinalIgnoreCase) ?? false);
            if (!hasCampus)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            var st = state.Trim();
            if (!college.States.Any(s => s.Contains(st, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }

    private static ScrapInstitute MapScrapInstitute(SqlDataReader reader)
    {
        return new ScrapInstitute
        {
            ScrappingId = reader.GetInt32(reader.GetOrdinal("ScrappingId")),
            InstituteName = ReadString(reader, "InstituteName"),
            Logo = ReadString(reader, "Logo"),
            WebsiteURL = ReadString(reader, "WebsiteURL"),
            Campus = ReadString(reader, "Campus"),
            State = ReadString(reader, "State"),
            City = ReadString(reader, "City"),
        };
    }

    private static List<string> SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        return value
            .Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private sealed class ScrapInstitute
    {
        public int ScrappingId { get; set; }
        public string? InstituteName { get; set; }
        public string? Logo { get; set; }
        public string? WebsiteURL { get; set; }
        public string? Campus { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
    }
}
