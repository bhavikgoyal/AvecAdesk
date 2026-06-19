using AvecADeskApi.Model.InstituteScrapping;

namespace AvecADeskApi.Interfaces;

public interface IInstituteScrappingRepository
{
    Task<List<InstituteScrappingResponse>> GetAllAsync();
    Task<InstituteScrappingResponse?> GetByIdAsync(int scrappingId);
    Task<int> CreateAsync(InstituteScrappingUpsertRequest request);
    Task<List<InstituteScrappingResponse>> CreateManyAsync(IEnumerable<InstituteScrappingUpsertRequest> requests);
    Task<bool> UpdateAsync(int scrappingId, InstituteScrappingUpsertRequest request);
    Task<bool> DeleteAsync(int scrappingId);
}
