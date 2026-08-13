using AvecADeskApi.Model.UserActivity;

namespace AvecADeskApi.Interfaces
{
    public interface IUserActivityReportRepository
    {
        Task<List<UserActivityResponse>> GetWorkReportAsync(
            DateTime fromDate,
            DateTime toDate,
            string? employeeName = null);
    }
}
