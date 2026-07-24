using AvecADeskApi.Model.College;

namespace AvecADeskApi.Interfaces;

public interface ICollegeRepository
{
    Task<CollegeFilterOptionsResponse> GetFilterOptionsAsync();
    Task<List<CollegeSummaryResponse>> SearchCollegesAsync(
        string? query,
        string? campus,
        string? state,
        int? topCount,
        bool? topCollegesOnly);
}
