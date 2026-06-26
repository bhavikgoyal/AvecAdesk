using AvecADeskApi.Model.Vendor;

namespace AvecADeskApi.Interfaces;

public interface IVendorRepository
{
    Task<List<VendorResponse>> GetVendorsAsync(string? status);
    Task<VendorResponse?> GetVendorByIdAsync(int vendorId);
    Task<VendorResponse?> GetVendorByEmailAsync(string email, int? excludeVendorId = null);
    Task<int> RegisterVendorAsync(VendorRegisterRequest request);
    Task<string> EnsureVendorCodeAsync(int vendorId);
    Task<bool> UpdateVendorAsync(int vendorId, VendorUpdateRequest request);
    Task<bool> UpdateVendorStatusAsync(int vendorId, string status);
    Task<bool> DeleteVendorAsync(int vendorId);
    Task<VendorResponse?> ApproveVendorAsync(int vendorId, int? approvedByUserId);
    Task<VendorAgreementResponse?> GetVendorAgreementAsync(int vendorId);
    Task<int> UploadVendorAgreementAsync(int vendorId, VendorAgreementUploadRequest request, string agreementPath, int? uploadedByUserId);
}
