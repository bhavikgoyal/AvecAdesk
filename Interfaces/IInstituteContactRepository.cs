using AvecADeskApi.Model.InstituteContact;

namespace AvecADeskApi.Interfaces;

public interface IInstituteContactRepository
{
    Task<IEnumerable<InstituteContactDto>> GetByInstituteIdAsync(int instituteId);
    Task<InstituteContactDto?> GetByIdAsync(int instituteId, int contactId);
    Task<int> CreateAsync(int instituteId, InstituteContactUpsertRequest request);
    Task<bool> UpdateAsync(int instituteId, int contactId, InstituteContactUpsertRequest request);
    Task<bool> DeleteAsync(int instituteId, int contactId);
}