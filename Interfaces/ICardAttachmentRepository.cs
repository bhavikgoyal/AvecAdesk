using AvecADeskApi.DTOs.Card;

namespace AvecADeskApi.Interfaces
{
    public interface ICardAttachmentRepository
    {
        Task<List<CardAttachmentResponse>> GetByCardIdAsync(int cardId);
        Task<CardAttachmentResponse?> CreateAsync(NewCardAttachment attachment, int? userId);
        Task<CardAttachmentResponse?> UpdateAsync(int attachmentId, UpdateCardAttachmentRequest request);
        Task<DeleteCardAttachmentResult> DeleteAsync(int attachmentId);
        Task<Dictionary<int, int>> GetCountsByCardIdsAsync(IEnumerable<int> cardIds);
    }
}
