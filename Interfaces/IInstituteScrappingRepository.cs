using AvecADeskApi.Model.InstituteScrapping;

namespace AvecADeskApi.Interfaces;

public interface IInstituteScrappingRepository
{
    Task<List<InstituteScrappingResponse>> GetAllAsync(string? instituteName = null);
    Task<InstituteScrappingResponse?> GetByIdAsync(int scrappingId);
    Task<int> CreateAsync(InstituteScrappingUpsertRequest request);
    Task<InstituteScrappingManualCreateResult> ManualCreateAsync(InstituteScrappingUpsertRequest request, int? vendorId = null);
    Task<List<InstituteScrappingResponse>> CreateManyAsync(IEnumerable<InstituteScrappingUpsertRequest> requests);
    Task<bool> UpdateAsync(int scrappingId, InstituteScrappingUpsertRequest request);
    Task<bool> DeleteAsync(int scrappingId);
}
