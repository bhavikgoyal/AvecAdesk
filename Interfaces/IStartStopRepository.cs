using AvecADeskApi.Models;

namespace AvecADeskApi.IRepository
{
    public interface IStartStopRepository
    {
        Task<int> InsertAsync(StartStop model);
        Task UpdateAsync(StartStop model);
        Task<List<StartStop>> GetAllByUserIdAsync(int userId);
        Task<List<StartStop>> GetAllByUserGetallAsync();
        Task UpdateStopTimeForTodayAsync();
    }
}
