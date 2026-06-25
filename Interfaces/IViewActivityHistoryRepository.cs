using AvecADeskApi.Model.ViewActivityHistory;

namespace AvecADeskApi.Interfaces
{
    public interface IViewActivityHistoryRepository
    {
        Task<List<ViewActivityHistoryResponse>> GetActivityHistoryByUserAsync(int userId, DateTime date);
    }
}
