using AvecADeskApi.Model.ViewActivityHistory;

namespace AIMonitoringApi.IRepository
{
    public interface IViewActivityHistory
    {
        Task<List<ViewActivityHistoryResponse>> GetActivityHistoryByUserAsync(int userId, DateTime date);
    }
}