using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.Vendor;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Vendors;

public class VendorOnboardingRepository : IVendorOnboardingRepository
{
    private readonly SqlDbHelper _db;

    public VendorOnboardingRepository(SqlDbHelper db)
    {
        _db = db;
    }

    public async Task<VendorOnboardingStepResponse> SaveCompanyInfoAsync(int userId, VendorCompanyInfoRequest request)
    {
        var vendorIdParam = new SqlParameter("@VendorId", SqlDbType.Int)
        {
            Direction = ParameterDirection.InputOutput,
            Value = request.VendorId.HasValue && request.VendorId.Value > 0
                ? request.VendorId.Value
                : DBNull.Value
        };

        var businessIdParam = new SqlParameter("@BusinessId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_SaveVendorCompanyInfo", cmd =>
        {
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.Add(vendorIdParam);
            cmd.Parameters.AddWithValue("@LegalBusinessName", request.LegalBusinessName);
            cmd.Parameters.AddWithValue("@TradingName", (object?)request.TradingName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@YearEstablished", (object?)request.YearEstablished ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CompanyRegNumber", (object?)request.CompanyRegistrationNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CountryOfRegistration", (object?)request.CountryOfRegistration ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RegisteredAddress", (object?)request.RegisteredOfficeAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OperationalAddress", (object?)request.OperationalOfficeAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Website", (object?)request.Website ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LinkedInProfile", (object?)request.LinkedInProfile ?? DBNull.Value);
            cmd.Parameters.Add(businessIdParam);
        });

        return new VendorOnboardingStepResponse
        {
            VendorId = (int)vendorIdParam.Value,
            BusinessId = businessIdParam.Value == DBNull.Value ? null : (int)businessIdParam.Value,
            Message = "Company information saved successfully."
        };
    }

    public async Task<VendorOnboardingStepResponse> SaveContactsAsync(VendorContactsRequest request)
    {
        await _db.ExecuteNonQueryAsync("sp_SaveVendorContacts", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@PrimaryContactName", request.PrimaryContactName);
            cmd.Parameters.AddWithValue("@PrimaryContactDesignation", (object?)request.PrimaryContactDesignation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PrimaryContactEmail", (object?)request.PrimaryContactEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PrimaryContactMobile", (object?)request.PrimaryContactMobile ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SecondaryContactName", (object?)request.SecondaryContactName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SecondaryContactEmail", (object?)request.SecondaryContactEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SecondaryContactNumber", (object?)request.SecondaryContactNumber ?? DBNull.Value);
        });

        return new VendorOnboardingStepResponse
        {
            VendorId = request.VendorId,
            Message = "Contact details saved successfully."
        };
    }

    public async Task<VendorOnboardingStepResponse> SaveBusinessProfileAsync(VendorBusinessProfileRequest request)
    {
        await _db.ExecuteNonQueryAsync("sp_SaveVendorBusinessProfile", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@BusinessId", (object?)request.BusinessId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BusinessType", request.BusinessType);
            cmd.Parameters.AddWithValue("@BusinessTypeOther", (object?)request.BusinessTypeOther ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmployeeCount", (object?)request.NumberOfEmployees ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CounselorCount", (object?)request.NumberOfCounselors ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OfficeCount", (object?)request.NumberOfOffices ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@YearsExperience", (object?)request.YearsOfExperience ?? DBNull.Value);
        });

        return new VendorOnboardingStepResponse
        {
            VendorId = request.VendorId,
            BusinessId = request.BusinessId,
            Message = "Business profile saved successfully."
        };
    }

    public async Task<VendorOnboardingStepResponse> SaveMarketsAsync(VendorMarketsRequest request)
    {
        var destinationCountries = BuildDestinationCountries(
            request.DestinationCountries,
            request.DestinationCountriesOther);

        var marketIdParam = new SqlParameter("@MarketId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_SaveVendorMarkets", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@PrimarySourceCountries", (object?)request.PrimaryStudentSourceCountries ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SecondaryMarkets", (object?)request.SecondaryMarkets ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Top5Institutions", (object?)request.Top5Institutions ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DestinationCountries", (object?)destinationCountries ?? DBNull.Value);
            cmd.Parameters.Add(marketIdParam);
        });

        return new VendorOnboardingStepResponse
        {
            VendorId = request.VendorId,
            MarketId = marketIdParam.Value == DBNull.Value ? null : (int)marketIdParam.Value,
            Message = "Recruitment markets saved successfully."
        };
    }

    private static string? BuildDestinationCountries(List<string>? countries, string? other)
    {
        if (countries == null || countries.Count == 0)
            return null;

        var values = countries.ToList();
        if (values.Contains("Other", StringComparer.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(other))
        {
            var index = values.FindIndex(c => string.Equals(c, "Other", StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                values[index] = $"Other: {other.Trim()}";
        }

        return string.Join(", ", values);
    }

    public async Task<VendorOnboardingStepResponse> SavePerformanceAsync(VendorPerformanceRequest request)
    {
        var performanceIdParam = new SqlParameter("@PerformanceId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_SaveVendorPerformance", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@YearRef", DBNull.Value);
            cmd.Parameters.AddWithValue("@StudentsRecruitedLastYear", (object?)request.StudentsRecruitedLastYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ExpectedStudentsNext12M", (object?)request.ExpectedStudentsNext12Months ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@VisaSuccessRate", (object?)request.VisaSuccessRate ?? DBNull.Value);
            cmd.Parameters.Add(performanceIdParam);
        });

        return new VendorOnboardingStepResponse
        {
            VendorId = request.VendorId,
            PerformanceId = performanceIdParam.Value == DBNull.Value ? null : (int)performanceIdParam.Value,
            Message = "Performance data saved successfully."
        };
    }

    public async Task<VendorOnboardingStepResponse> SaveComplianceAsync(VendorComplianceRequest request)
    {
        var agreements = request.ComplianceAgreements ?? new List<string>();
        var complianceIdParam = new SqlParameter("@ComplianceId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_SaveVendorCompliance", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@IsRegisteredWithBody", ParseYesNo(request.RegisteredWithRegulatoryBody));
            cmd.Parameters.AddWithValue("@RegulatoryBodyDetails", (object?)request.RegulatoryBodyDetails ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HasCertifiedCounselors", ParseYesNo(request.CertifiedCounselors));
            cmd.Parameters.AddWithValue("@HasFraudHistory", ParseYesNo(request.VisaFraudHistory));
            cmd.Parameters.AddWithValue("@FraudHistoryDetails", (object?)request.VisaFraudExplanation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AgreesEthicalRecruitment", agreements.Contains("Ethical recruitment practices"));
            cmd.Parameters.AddWithValue("@AgreesDataProtection", agreements.Contains("Student data protection laws"));
            cmd.Parameters.AddWithValue("@AgreesImmigrationRegs", agreements.Contains("Immigration regulations"));
            cmd.Parameters.Add(complianceIdParam);
        });

        return new VendorOnboardingStepResponse
        {
            VendorId = request.VendorId,
            ComplianceId = complianceIdParam.Value == DBNull.Value ? null : (int)complianceIdParam.Value,
            Message = "Compliance details saved successfully."
        };
    }

    public async Task<VendorOnboardingStepResponse> SaveMarketingCapabilityAsync(VendorMarketingCapabilityRequest request)
    {
        var marketingChannels = BuildMarketingChannels(request.MarketingChannels, request.MarketingChannelsOther);

        await _db.ExecuteNonQueryAsync("sp_SaveVendorMarketingCapability", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@BusinessId", (object?)request.BusinessId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ConductsSeminars", ParseYesNo(request.ConductsSeminars));
            cmd.Parameters.AddWithValue("@MarketingChannels", (object?)marketingChannels ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HasVisaProcessingSupport", ParseYesNo(request.InHouseVisaSupport));
        });

        return new VendorOnboardingStepResponse
        {
            VendorId = request.VendorId,
            BusinessId = request.BusinessId,
            Message = "Marketing capability saved successfully."
        };
    }

    public async Task<VendorOnboardingStepResponse> SaveCommercialTermsAsync(VendorCommercialTermsRequest request)
    {
        await _db.ExecuteNonQueryAsync("sp_SaveVendorCommercialTerms", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@BusinessId", (object?)request.BusinessId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PreferredPaymentTerms", request.PreferredPaymentTerms);
        });

        return new VendorOnboardingStepResponse
        {
            VendorId = request.VendorId,
            BusinessId = request.BusinessId,
            Message = "Commercial terms saved successfully."
        };
    }

    public async Task<VendorOnboardingStepResponse> SaveBankingAsync(VendorBankingRequest request)
    {
        var bankingIdParam = new SqlParameter("@BankingId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_SaveVendorBanking", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@BankName", (object?)request.BankName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountName", (object?)request.AccountName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountNumber", (object?)request.AccountNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SwiftCode", (object?)request.SwiftCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BankCountry", (object?)request.BankCountry ?? DBNull.Value);
            cmd.Parameters.Add(bankingIdParam);
        });

        return new VendorOnboardingStepResponse
        {
            VendorId = request.VendorId,
            BankingId = bankingIdParam.Value == DBNull.Value ? null : (int)bankingIdParam.Value,
            Message = "Banking details saved successfully."
        };
    }

    public async Task<int> SaveDocumentAsync(int vendorId, VendorDocumentSaveRequest request)
    {
        var documentIdParam = new SqlParameter("@DocumentId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_SaveVendorDocument", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", vendorId);
            cmd.Parameters.AddWithValue("@DocumentType", request.DocumentType);
            cmd.Parameters.AddWithValue("@FileName", (object?)request.FileName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FileUrl", request.FileUrl);
            cmd.Parameters.AddWithValue("@FileSizeKB", (object?)request.FileSizeKB ?? DBNull.Value);
            cmd.Parameters.Add(documentIdParam);
        });

        return (int)documentIdParam.Value;
    }

    public async Task<VendorOnboardingStepResponse> SubmitDeclarationAsync(VendorDeclarationRequest request)
    {
        var agreements = request.DeclarationItems ?? new List<string>();
        var declarationIdParam = new SqlParameter("@DeclarationId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var vendorCodeParam = new SqlParameter("@VendorCode", SqlDbType.NVarChar, 50)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_SubmitVendorDeclaration", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", request.VendorId);
            cmd.Parameters.AddWithValue("@SignatoryName", request.AuthorizedSignatoryName);
            cmd.Parameters.AddWithValue("@SignatureUrl", request.Signature);
            cmd.Parameters.AddWithValue("@InfoIsCorrect", agreements.Contains("All information provided is true and correct"));
            cmd.Parameters.AddWithValue("@AgreesToPartnerReqs", agreements.Contains("We agree to comply with all applicable laws and partner requirements"));
            cmd.Parameters.AddWithValue("@UnderstandsDueDiligence", agreements.Contains("We understand that approval is subject to due diligence"));
            cmd.Parameters.AddWithValue("@DeclaredAt", (object?)request.DeclarationDate ?? DBNull.Value);
            cmd.Parameters.Add(declarationIdParam);
            cmd.Parameters.Add(vendorCodeParam);
        });

        return new VendorOnboardingStepResponse
        {
            VendorId = request.VendorId,
            DeclarationId = declarationIdParam.Value == DBNull.Value ? null : (int)declarationIdParam.Value,
            VendorCode = vendorCodeParam.Value == DBNull.Value ? null : vendorCodeParam.Value.ToString(),
            Message = "Application submitted successfully."
        };
    }

    private static bool ParseYesNo(string? value)
    {
        return string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string? BuildMarketingChannels(List<string>? channels, string? other)
    {
        if (channels == null || channels.Count == 0)
            return null;

        var values = channels.ToList();
        if (values.Contains("Other", StringComparer.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(other))
        {
            var index = values.FindIndex(c => string.Equals(c, "Other", StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                values[index] = $"Other: {other.Trim()}";
        }

        return string.Join(", ", values);
    }
}
