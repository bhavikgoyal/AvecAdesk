using AvecADeskApi.DTOs.Card;

namespace AvecADeskApi.Interfaces
{
    public interface ICardCoverRepository
    {
        Task<List<CardCoverColorResponse>> GetColorsAsync();
        Task<CardCoverResponse?> GetByCardIdAsync(int cardId);
        Task<Dictionary<int, CardCoverResponse>> GetByCardIdsAsync(IEnumerable<int> cardIds);
        Task<CardCoverResponse?> SaveAsync(SaveCardCoverRequest request, int? userId);
        Task<bool> RemoveAsync(int cardId);
    }
}
