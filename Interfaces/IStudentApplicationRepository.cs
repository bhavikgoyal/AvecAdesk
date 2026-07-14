using AvecADeskApi.Model.Student;

namespace AvecADeskApi.Interfaces
{
    public interface IStudentApplicationRepository
    {
        Task<List<StudentApplicationDetailsModel>> GetStudentApplicationsAsync(string? search, int pagenumber, int pageSize, int? vendorId = null);
        Task<StudentApplicationResponse?> GetApplicationByIdAsync(Guid applicationId);
        Task<Guid> CreateApplicationAsync(Guid studentId, StudentApplicationCreateRequest request);
        //Task<Guid> CreateApplicationAsync(StudentApplicationCreateRequest request);
        //Task<bool> SaveApplicationDetailAsync(Guid applicationId, ApplicationDetailRequest request);
        Task<ApplicationDetailResponse?> SaveApplicationDetailAsync(Guid applicationId,ApplicationDetailRequest request);
        Task<int?> GetVendorIdByUserIdAsync(int userId);
        Task<ApplicationDocumentResponse> UploadDocumentAsync(Guid applicationId, ApplicationDocumentRequest request, string fileUrl);
        Task<bool> DeleteDocumentAsync(Guid documentId);
        Task<bool> SaveDeclarationAsync(Guid applicationId, DeclarationRequest request);
        Task<bool> SubmitApplicationAsync(Guid applicationId);
    }
}
