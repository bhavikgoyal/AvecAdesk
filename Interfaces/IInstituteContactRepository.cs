using AvecADeskApi.Model.InstituteContact;

namespace AvecADeskApi.Interfaces;

public interface IInstituteContactRepository
{
    Task<InstituteContactDto?> GetByInstituteIdAsync(int instituteId);
    Task UpsertAsync(int instituteId, InstituteContactUpsertRequest request);
}