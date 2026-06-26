using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.Vendor;

namespace AvecADeskApi.Services;

public class VendorOnboardingAdminService
{
    private readonly IVendorOnboardingRepository _repository;

    public VendorOnboardingAdminService(IVendorOnboardingRepository repository)
    {
        _repository = repository;
    }

    public async Task SaveAsync(int vendorId, VendorOnboardingAdminSaveRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetOnboardingAsync(vendorId)
            ?? throw new InvalidOperationException("Vendor onboarding record not found.");

        var userId = await _repository.GetVendorUserIdAsync(vendorId);
        if (userId == null || userId <= 0)
            throw new InvalidOperationException("Vendor user is missing. Cannot save company information.");

        if (!string.IsNullOrWhiteSpace(request.LegalBusinessName))
        {
            await _repository.SaveCompanyInfoAsync(userId.Value, new VendorCompanyInfoRequest
            {
                VendorId = vendorId,
                LegalBusinessName = request.LegalBusinessName.Trim(),
                TradingName = request.TradingName,
                YearEstablished = request.YearEstablished,
                CompanyRegistrationNumber = request.CompanyRegistrationNumber,
                CountryOfRegistration = request.CountryOfRegistration,
                RegisteredOfficeAddress = request.RegisteredOfficeAddress,
                OperationalOfficeAddress = request.OperationalOfficeAddress,
                Website = request.Website,
                LinkedInProfile = request.LinkedInProfile,
            });
        }

        if (!string.IsNullOrWhiteSpace(request.PrimaryContactName))
        {
            await _repository.SaveContactsAsync(new VendorContactsRequest
            {
                VendorId = vendorId,
                PrimaryContactName = request.PrimaryContactName.Trim(),
                PrimaryContactDesignation = request.PrimaryContactDesignation,
                PrimaryContactEmail = request.PrimaryContactEmail,
                PrimaryContactMobile = request.PrimaryContactMobile,
                SecondaryContactName = request.SecondaryContactName,
                SecondaryContactEmail = request.SecondaryContactEmail,
                SecondaryContactNumber = request.SecondaryContactNumber,
            });
        }

        if (!string.IsNullOrWhiteSpace(request.BusinessType))
        {
            await _repository.SaveBusinessProfileAsync(new VendorBusinessProfileRequest
            {
                VendorId = vendorId,
                BusinessId = request.BusinessId ?? existing.BusinessId,
                BusinessType = request.BusinessType,
                BusinessTypeOther = request.BusinessTypeOther,
                NumberOfEmployees = request.NumberOfEmployees,
                NumberOfCounselors = request.NumberOfCounselors,
                NumberOfOffices = request.NumberOfOffices,
                YearsOfExperience = request.YearsOfExperience,
            });
        }

        if (!string.IsNullOrWhiteSpace(request.PrimaryStudentSourceCountries))
        {
            await _repository.SaveMarketsAsync(new VendorMarketsRequest
            {
                VendorId = vendorId,
                PrimaryStudentSourceCountries = request.PrimaryStudentSourceCountries,
                SecondaryMarkets = request.SecondaryMarkets,
                Top5Institutions = request.Top5Institutions,
                DestinationCountries = request.DestinationCountries,
                DestinationCountriesOther = request.DestinationCountriesOther,
            });
        }

        if (request.StudentsRecruitedLastYear != null || request.ExpectedStudentsNext12Months != null || request.VisaSuccessRate != null)
        {
            await _repository.SavePerformanceAsync(new VendorPerformanceRequest
            {
                VendorId = vendorId,
                StudentsRecruitedLastYear = request.StudentsRecruitedLastYear,
                ExpectedStudentsNext12Months = request.ExpectedStudentsNext12Months,
                VisaSuccessRate = request.VisaSuccessRate,
            });
        }

        if (!string.IsNullOrWhiteSpace(request.RegisteredWithRegulatoryBody))
        {
            await _repository.SaveComplianceAsync(new VendorComplianceRequest
            {
                VendorId = vendorId,
                RegisteredWithRegulatoryBody = request.RegisteredWithRegulatoryBody,
                RegulatoryBodyDetails = request.RegulatoryBodyDetails,
                CertifiedCounselors = request.CertifiedCounselors ?? "no",
                VisaFraudHistory = request.VisaFraudHistory ?? "no",
                VisaFraudExplanation = request.VisaFraudExplanation,
                ComplianceAgreements = request.ComplianceAgreements ?? new List<string>(),
            });
        }

        if (!string.IsNullOrWhiteSpace(request.ConductsSeminars))
        {
            await _repository.SaveMarketingCapabilityAsync(new VendorMarketingCapabilityRequest
            {
                VendorId = vendorId,
                BusinessId = request.BusinessId ?? existing.BusinessId,
                ConductsSeminars = request.ConductsSeminars,
                MarketingChannels = request.MarketingChannels,
                MarketingChannelsOther = request.MarketingChannelsOther,
                InHouseVisaSupport = request.InHouseVisaSupport ?? "no",
            });
        }

        if (!string.IsNullOrWhiteSpace(request.PreferredPaymentTerms))
        {
            await _repository.SaveCommercialTermsAsync(new VendorCommercialTermsRequest
            {
                VendorId = vendorId,
                BusinessId = request.BusinessId ?? existing.BusinessId,
                PreferredPaymentTerms = request.PreferredPaymentTerms,
            });
        }

        if (!string.IsNullOrWhiteSpace(request.BankName)
            || !string.IsNullOrWhiteSpace(request.AccountName)
            || !string.IsNullOrWhiteSpace(request.AccountNumber))
        {
            await _repository.SaveBankingAsync(new VendorBankingRequest
            {
                VendorId = vendorId,
                BankName = request.BankName,
                AccountName = request.AccountName,
                AccountNumber = request.AccountNumber,
                SwiftCode = request.SwiftCode,
                BankCountry = request.BankCountry,
            });
        }

        if (!existing.IsLinkExpired
            && !string.IsNullOrWhiteSpace(request.AuthorizedSignatoryName)
            && !string.IsNullOrWhiteSpace(request.Signature)
            && request.DeclarationDate != null)
        {
            await _repository.SubmitDeclarationAsync(new VendorDeclarationRequest
            {
                VendorId = vendorId,
                AuthorizedSignatoryName = request.AuthorizedSignatoryName.Trim(),
                Signature = request.Signature.Trim(),
                DeclarationDate = request.DeclarationDate,
                DeclarationItems = request.DeclarationItems ?? new List<string>(),
            });
        }
    }
}
