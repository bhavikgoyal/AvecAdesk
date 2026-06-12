using AvecADeskApi.Model.Institute;

namespace AvecADeskApi.Interfaces;

public interface IInstituteRepository
{
    Task<List<InstituteResponse>> SearchInstitutesAsync(string? name, string? city, string? service);
    Task<InstituteResponse?> GetInstituteByIdAsync(int instituteId);
    Task<List<InstituteResponse>> GetInstitutesAdminAsync(string? status);
    Task<int> CreateInstituteAsync(InstituteCreateRequest request);
    Task<bool> UpdateInstituteAsync(int instituteId, InstituteUpdateRequest request);
    Task<bool> UpdateInstituteStatusAsync(int instituteId, string status);
    Task<bool> PublishInstituteAsync(int instituteId);
}
