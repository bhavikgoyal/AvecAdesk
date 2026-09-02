using AvecADeskApi.Model.InstituteCredential;

namespace AvecADeskApi.Interfaces;

public interface IInstituteCredentialRepository
{
    Task<IEnumerable<InstituteCredentialDto>> GetByInstituteIdAsync(int instituteId);
    Task<InstituteCredentialDto?> GetByIdAsync(int instituteId, int credentialId);
    Task<int> CreateAsync(int instituteId, InstituteCredentialUpsertRequest request);
    Task<bool> UpdateAsync(int instituteId, int credentialId, InstituteCredentialUpsertRequest request);
    Task<bool> DeleteAsync(int instituteId, int credentialId);
}