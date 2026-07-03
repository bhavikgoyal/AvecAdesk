using AvecADeskApi.Model.AgrrementTemplate;

namespace AvecADeskApi.Interfaces;

public interface IAgrrementTemplateRepository
{
    Task<List<AgrrementTemplateResponse>> GetAgrrementTemplatesAsync();
    Task<AgrrementTemplateResponse?> GetAgrrementTemplateByIdAsync(int templateId);
    Task<int> CreateAgrrementTemplateAsync(AgrrementTemplateCreateRequest request);
    Task<bool> UpdateAgrrementTemplateAsync(int templateId, AgrrementTemplateUpdateRequest request);
    Task<bool> DeleteAgrrementTemplateAsync(int templateId);
}
