using AvecADeskApi.DTOs.Card;

namespace AvecADeskApi.Interfaces
{
    public interface ICardRepository
    {
        Task<List<BoardColumnResponse>> GetBoardCardsAsync(
            string? searchText, int? assignedUserId, DateTime? fromDate, DateTime? toDate);

        Task<int> CreateCardAsync(CreateCardRequest request, int createdUserId);

        Task UpdateCardAsync(UpdateCardRequest request);

        Task MoveCardAsync(MoveCardRequest request);

        Task DeleteCardAsync(int cardId);
    }
}

