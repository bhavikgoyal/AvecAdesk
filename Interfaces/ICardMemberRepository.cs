using AvecADeskApi.DTOs.Card;

namespace AvecADeskApi.Interfaces
{
    public interface ICardMemberRepository
    {
        Task<List<CardMemberResponse>> GetCardMembersAsync(int cardId);
        Task<Dictionary<int, List<CardMemberResponse>>> GetByCardIdsAsync(IEnumerable<int> cardIds);
        Task AddCardMemberAsync(int cardId, int userId);
        Task RemoveCardMemberAsync(int cardId, int userId);
    }
}