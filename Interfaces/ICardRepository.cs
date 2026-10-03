using AvecADeskApi.DTOs.Card;

namespace AvecADeskApi.Interfaces
{
    public interface ICardRepository
    {
        Task<List<BoardColumnResponse>> GetBoardCardsAsync(
            string? searchText, int? assignedUserId, DateTime? fromDate, DateTime? toDate);

        Task<List<BoardColumnResponse>> GetMyAssignedBoardCardsAsync(
            int assignedUserId, string? searchText, DateTime? fromDate, DateTime? toDate);

        Task<int> CreateCardAsync(CreateCardRequest request, int createdUserId);

        Task<List<CardResponse>> GetCardsByBoardIdAsync(
    int boardId,
    string? searchText,
    int? assignedUserId,
    DateTime? fromDate,
    DateTime? toDate);

        Task UpdateCardAsync(UpdateCardRequest request);

        Task MoveCardAsync(MoveCardRequest request);
        Task<MoveCardToListResponse?> MoveCardToListAsync(MoveCardToListRequest request);

        Task DeleteCardAsync(int cardId);
    }
}

