using AvecADeskApi.DTOs.Card;

namespace AvecADeskApi.Interfaces
{
    public interface ICardCommentRepository
    {
        Task<List<CardCommentResponse>> GetByCardIdAsync(int cardId);
        Task<CardCommentResponse?> CreateAsync(int cardId, int userId, string commentText);
        Task<CardCommentResponse?> UpdateAsync(int commentId, int userId, string commentText);
        Task<bool> DeleteAsync(int commentId, int userId);
        Task<List<CardActivityResponse>> GetActivityByCardIdAsync(int cardId);
        Task LogActivityAsync(LogCardActivityRequest request, int userId);
    }
}
