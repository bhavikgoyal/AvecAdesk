namespace AvecADeskApi.Interfaces
{
    public interface IBoardRepository
    {
        Task<int> CreateBoardAsync(string boardName, int createdByUserId);
        Task<List<(int BoardID, string BoardName)>> GetBoardsByUserIdAsync(int userId);
        Task<bool> UpdateBoardNameAsync(int boardId, string boardName, int userId);
    }
}