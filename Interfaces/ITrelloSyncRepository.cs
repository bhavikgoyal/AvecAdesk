using AvecADeskApi.DTOs.Trello;

namespace AvecADeskApi.Interfaces
{
    public interface ITrelloSyncRepository
    {
        Task<List<TrelloBoardLinkResponse>> GetBoardLinksAsync();
        Task<TrelloBoardLinkResponse?> GetBoardLinkAsync(int localBoardId);
        Task<TrelloBoardLinkResponse?> SaveBoardLinkAsync(int localBoardId, string trelloBoardId, string? trelloBoardName, string initialWinner, int userId);
        Task<bool> DeleteBoardLinkAsync(int localBoardId);
        Task SetStatusAsync(int localBoardId, string? status, bool activate);
        Task<List<TrelloMapRow>> GetMapsForBoardAsync(int localBoardId);
        Task SaveMapAsync(string entityType, int localId, string trelloId, int? localBoardId, string? syncHash);

        Task<List<LocalBoardRow>> GetLocalBoardsAsync();
        Task<int> CleanupOrphanLinksAsync();
        Task<List<(int LocalBoardID, string TrelloListID)>> GetBoardListMapsAsync();
        Task<int?> GetCardBoardIdAsync(int cardId);
        Task<int?> GetOwnerUserIdByRoleAsync(int roleId);

        Task<List<StoredWebhookRow>> GetWebhooksAsync();
        Task SaveWebhookAsync(string webhookId, string idModel, string callbackUrl);
        Task DeleteWebhookAsync(string webhookId);
    }
}
