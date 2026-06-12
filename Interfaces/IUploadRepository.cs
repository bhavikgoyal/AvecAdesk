using AvecADeskApi.Model.Upload;

namespace AvecADeskApi.Interfaces;

public interface IUploadRepository
{
    Task<List<InstituteUploadResponse>> GetUploadsAsync(int? instituteId);
    Task<InstituteUploadResponse?> GetUploadByIdAsync(int uploadId);
    Task<int> UploadInstituteExcelAsync(int instituteId, int uploadedByUserId, string filePath);
    Task<UploadDiffResponse?> GetUploadDiffAsync(int uploadId);
    Task<bool> ReconcileUploadAsync(int uploadId);
}
