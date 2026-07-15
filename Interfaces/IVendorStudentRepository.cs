using AvecADeskApi.Model.VendorStudent;

namespace AvecADeskApi.Interfaces
{
  public interface IVendorStudentRepository
  {
    Task<int> CreateVendorStudentAsync(SaveVendorStudentRequest request);
    Task UpdateAgentDetailsAsync(int studentId, UpdateVendorStudentAgentRequest request);
    Task UpdateImmigrationAsync(int studentId, UpdateVendorStudentImmigrationRequest request);
    Task UpdateEnglishAsync(int studentId, UpdateVendorStudentEnglishRequest request);
    Task SaveEducationHistoryAsync(int studentId, SaveStudentEducationHistoryRequest request);
    Task<int> SaveDocumentAsync(int studentId, string category, string docType, string filePath);
    Task UpdateChecklistAsync(int studentId, UpdateVendorStudentChecklistRequest request);
    Task UpdateDeclarationAsync(int studentId, UpdateVendorStudentDeclarationRequest request);
    Task SubmitAsync(int studentId);
    Task<List<VendorStudentHistoryItem>> GetHistoryAsync(int vendorId, string? search, int pageNumber, int pageSize);
  }
}
