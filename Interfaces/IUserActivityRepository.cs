using AvecADeskApi.Model.UserActivity;

namespace AvecADeskApi.Interfaces
{
    public interface IUserActivityRepository
    {
     Task<List<UserActivityResponse>> GetWorkReportAsync(DateTime fromDate, DateTime toDate, string? employeeName = null);
    }
}
