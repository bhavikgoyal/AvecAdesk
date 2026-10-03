namespace AvecADeskApi.Interfaces
{
    public interface IBoardRepository
    {
        Task<int> CreateBoardAsync(string boardName, int createdByUserId);
        Task<List<(int BoardID, string BoardName)>> GetBoardsByUserIdAsync(int userId);
    }
}