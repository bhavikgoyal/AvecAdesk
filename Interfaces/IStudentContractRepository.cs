using AvecADeskApi.Model.StudentContract;

namespace AvecADeskApi.Interfaces;

public interface IStudentContractRepository
{
    Task<IEnumerable<StudentContract>> GetByStudentIdAsync(int studentId);
    Task<StudentContract?> GetByIdAsync(int studentId, int contractId);
    Task<int> CreateAsync(int studentId, StudentContractUpsertRequest request);
    Task<bool> UpdateAsync(int studentId, int contractId, StudentContractUpsertRequest request);
    Task<bool> DeleteAsync(int studentId, int contractId);
}