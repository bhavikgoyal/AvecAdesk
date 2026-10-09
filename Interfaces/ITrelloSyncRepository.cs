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

        Task<List<LocalChecklistRow>> GetBoardChecklistsAsync(int localBoardId);
        Task<int> SaveChecklistAsync(int? checklistId, int cardId, string title, string trelloChecklistId, DateTime? createdAt);
        Task<int> SaveChecklistItemAsync(int? checklistItemId, int checklistId, string itemName, bool isCompleted, int? position,
            string trelloItemId, string? syncHash, DateTime? createdAt);
        Task<List<LocalCommentRow>> GetBoardCommentsAsync(int localBoardId);
        Task<int> SaveCommentAsync(int? commentId, int cardId, int userId, string commentText, DateTime? createdAtUtc,
            string trelloCommentId, string? syncHash);
        Task<Dictionary<string, int>> GetTrelloUserMapAsync();
        Task<TrelloLocalRef?> GetLocalRefAsync(string entityType, int localId);
        Task DeleteLocalAsync(string entityType, int localId);
        Task AddTombstoneAsync(string entityType, string trelloId, string? trelloParentId, int? localBoardId);
        Task<List<TrelloTombstoneRow>> GetTombstonesAsync(int localBoardId);
        Task MarkTombstoneAsync(string trelloId, string? error);
        Task<string?> GetLastActivityIdAsync(int localBoardId);
        Task<bool> SaveActivityAsync(TrelloActivityImport activity);
        Task<List<LocalBoardLabelRow>> GetBoardLabelsAsync(int localBoardId);
        Task<int> SaveBoardLabelAsync(int? boardLabelId, int localBoardId, string labelName, string color);
        Task DeleteBoardLabelAsync(int boardLabelId);
        Task<List<(int CardID, int BoardLabelID)>> GetBoardCardLabelsAsync(int localBoardId);
        Task<List<int>> SetCardLabelsAsync(int cardId, IEnumerable<int> boardLabelIds);
        Task<List<LocalCardDetailsRow>> GetBoardCardDetailsAsync(int localBoardId);
        Task SetCardStartDateAsync(int cardId, DateTime? startDate);
        Task SaveCardCoverAsync(int cardId, string color, string size, string brightness);
        Task RemoveCardCoverAsync(int cardId);
    }
}
