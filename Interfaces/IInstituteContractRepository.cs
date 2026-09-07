using AvecADeskApi.Model.InstituteContract;

namespace AvecADeskApi.Interfaces;

public interface IInstituteContractRepository
{
    Task<IEnumerable<InstituteContractDto>> GetByInstituteIdAsync(int instituteId);
    Task<InstituteContractDto?> GetByIdAsync(int instituteId, int contractId);
    Task<int> CreateAsync(int instituteId, InstituteContractUpsertRequest request);
    Task<bool> UpdateAsync(int instituteId, int contractId, InstituteContractUpsertRequest request);
    Task<bool> DeleteAsync(int instituteId, int contractId);
}