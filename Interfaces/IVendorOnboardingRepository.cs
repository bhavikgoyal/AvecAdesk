using AvecADeskApi.Model.Vendor;

namespace AvecADeskApi.Interfaces;

public interface IVendorOnboardingRepository
{
    Task<VendorOnboardingStepResponse> SaveCompanyInfoAsync(int userId, VendorCompanyInfoRequest request);
    Task<VendorOnboardingStepResponse> SaveContactsAsync(VendorContactsRequest request);
    Task<VendorOnboardingStepResponse> SaveBusinessProfileAsync(VendorBusinessProfileRequest request);
    Task<VendorOnboardingStepResponse> SaveMarketsAsync(VendorMarketsRequest request);
    Task<VendorOnboardingStepResponse> SavePerformanceAsync(VendorPerformanceRequest request);
    Task<VendorOnboardingStepResponse> SaveComplianceAsync(VendorComplianceRequest request);
    Task<VendorOnboardingStepResponse> SaveMarketingCapabilityAsync(VendorMarketingCapabilityRequest request);
    Task<VendorOnboardingStepResponse> SaveCommercialTermsAsync(VendorCommercialTermsRequest request);
    Task<VendorOnboardingStepResponse> SaveBankingAsync(VendorBankingRequest request);
    Task<int> SaveDocumentAsync(int vendorId, VendorDocumentSaveRequest request);
    Task<VendorOnboardingStepResponse> SubmitDeclarationAsync(VendorDeclarationRequest request);
    Task<VendorOnboardingDataResponse?> GetOnboardingAsync(int vendorId);
    Task<bool> IsOnboardingLinkExpiredAsync(int vendorId);
    Task<int?> GetVendorUserIdAsync(int vendorId);
}
