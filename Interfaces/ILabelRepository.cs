using AvecADeskApi.DTOs.Label;

namespace AvecADeskApi.Interfaces
{
    public interface ILabelRepository
    {
        Task<List<LabelResponse>> GetByCardIdAsync(int cardId);
        Task<LabelResponse> CreateLabelAsync(CreateLabelRequest request);
        Task<bool> DeleteLabelAsync(int labelId);
        Task SyncCardColorAsync(int cardId);
    }
}
