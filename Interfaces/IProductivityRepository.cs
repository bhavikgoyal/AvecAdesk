using AvecADeskApi.Model.Productivity;

namespace AvecADeskApi.Interfaces
{
    public interface IProductivityRepository
    {
        Task<ProductivityDashboardResponse> GetDashboardAsync(DateTime fromDate, DateTime toDate, int trendDays);
    }
}
