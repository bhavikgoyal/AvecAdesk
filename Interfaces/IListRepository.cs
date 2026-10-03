namespace AvecADeskApi.Interfaces
{
    public interface IListRepository
    {
        Task<int> CreateListAsync(int boardId, string listName);
    }
}