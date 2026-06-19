using AvecADeskApi.Model.InstituteScrapping;

namespace AvecADeskApi.Interfaces;

public interface IInstituteScrappingService
{
    Task<InstituteScrappingRunResponse> RunScrapeAsync(InstituteScrappingRunRequest request);
}
