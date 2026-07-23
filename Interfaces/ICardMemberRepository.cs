using AvecADeskApi.DTOs.Card;

namespace AvecADeskApi.Interfaces
{
    public interface ICardMemberRepository
    {
        Task<List<CardMemberResponse>> GetCardMembersAsync(int cardId);
        Task AddCardMemberAsync(int cardId, int userId);
        Task RemoveCardMemberAsync(int cardId, int userId);
    }
}