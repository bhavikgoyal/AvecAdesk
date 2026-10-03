namespace AvecADeskApi.Interfaces
{
    public interface IListRepository
    {
        Task<int> CreateListAsync(int boardId, string listName);
        Task<List<(int ListID, int BoardID, string ListName, int Position)>> GetListsByBoardIdAsync(int boardId);
    }
}