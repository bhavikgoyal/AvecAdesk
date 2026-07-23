using AvecADeskApi.DTOs;
using AvecADeskApi.Model.Cardlist;

namespace AvecADeskApi.Interfaces
{
    public interface IChecklistRepository
    {
        Task<List<ChecklistModel>> GetByCardIdAsync(int cardId);
        Task<List<WeekChecklistItemModel>> GetWeekChecklistItemsAsync();
        Task<int> CreateChecklistAsync(CreateChecklistRequest request);
        Task<bool> DeleteChecklistAsync(int checklistId);
        Task<int> CreateChecklistItemAsync(CreateChecklistItemRequest request);
        Task<bool> UpdateChecklistItemStatusAsync(int checklistItemId, bool isCompleted);
        Task<bool> DeleteChecklistItemAsync(int checklistItemId);
        Task<bool> UpdateChecklistItemNameAsync(int checklistItemId, string itemName);
        //Task  UpdateChecklistItemAssigneeAsync(int checklistItemID, int assignedUserID);
    }
}
