using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.Vendor;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Vendors;

public class VendorOnboardingRepository : IVendorOnboardingRepository
{
    private readonly SqlDbHelper _db;

    private static readonly Dictionary<string, string> DocumentTypeToField = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RegCert"] = "companyRegistrationCertificate",
        ["DirectorID"] = "directorIdPassport",
        ["OfficePhoto"] = "officePhotos",
        ["Brochure"] = "businessProfile",
        ["PartnerAgreement"] = "existingPartnerAgreements",
        ["Other"] = "existingPartnerAgreements",
        // Legacy values saved before DB code mapping was added
        ["Company Registration Certificate"] = "companyRegistrationCertificate",
        ["Director ID / Passport"] = "directorIdPassport",
        ["Office Photos"] = "officePhotos",
        ["Business Profile / Brochure"] = "businessProfile",
        ["Existing Partner Agreements"] = "existingPartnerAgreements",
    };

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
            cmd.Parameters.AddWithValue("@BusinessType", MapBusinessTypeToDb(request.BusinessType));
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

    public async Task<VendorOnboardingDataResponse?> GetOnboardingAsync(int vendorId)
    {
        return await _db.ExecuteReaderCustomAsync(
            "sp_GetVendorOnboarding",
            cmd => cmd.Parameters.AddWithValue("@VendorId", vendorId),
            ReadOnboardingAsync);
    }

    public async Task<bool> IsOnboardingLinkExpiredAsync(int vendorId)
    {
        var data = await GetOnboardingAsync(vendorId);
        return data == null || data.IsLinkExpired;
    }

    public async Task<int?> GetVendorUserIdAsync(int vendorId)
    {
        var results = await _db.ExecuteReaderListAsync(
            "sp_GetVendorById",
            cmd => cmd.Parameters.AddWithValue("@VendorId", vendorId),
            reader =>
            {
                var ordinal = reader.GetOrdinal("UserId");
                return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
            });

        return results.FirstOrDefault();
    }

    private static async Task<VendorOnboardingDataResponse?> ReadOnboardingAsync(SqlDataReader reader)
    {
        if (!await reader.ReadAsync())
            return null;

        var data = new VendorOnboardingDataResponse
        {
            VendorId = reader.GetInt32(reader.GetOrdinal("VendorId")),
            VendorCode = GetNullableString(reader, "VendorCode"),
            IsLinkExpired = reader.GetInt32(reader.GetOrdinal("IsLinkExpired")) == 1,
            LegalBusinessName = GetNullableString(reader, "LegalBusinessName"),
            BusinessId = GetNullableInt(reader, "BusinessId"),
            TradingName = GetNullableString(reader, "TradingName"),
            YearEstablished = GetNullableShort(reader, "YearEstablished"),
            CompanyRegistrationNumber = GetNullableString(reader, "CompanyRegNumber"),
            CountryOfRegistration = GetNullableString(reader, "CountryOfRegistration"),
            RegisteredOfficeAddress = GetNullableString(reader, "RegisteredAddress"),
            OperationalOfficeAddress = GetNullableString(reader, "OperationalAddress"),
            Website = GetNullableString(reader, "Website"),
            LinkedInProfile = GetNullableString(reader, "LinkedInProfile"),
            BusinessType = MapBusinessTypeFromDb(GetNullableString(reader, "BusinessType")),
            BusinessTypeOther = GetNullableString(reader, "BusinessTypeOther"),
            NumberOfEmployees = GetNullableInt(reader, "EmployeeCount"),
            NumberOfCounselors = GetNullableInt(reader, "CounselorCount"),
            NumberOfOffices = GetNullableInt(reader, "OfficeCount"),
            YearsOfExperience = GetNullableInt(reader, "YearsExperience"),
            MarketId = GetNullableInt(reader, "MarketId"),
            PrimaryStudentSourceCountries = GetNullableString(reader, "PrimarySourceCountries"),
            SecondaryMarkets = GetNullableString(reader, "SecondaryMarkets"),
            Top5Institutions = GetNullableString(reader, "Top5Institutions"),
            PerformanceId = GetNullableInt(reader, "PerformanceId"),
            StudentsRecruitedLastYear = GetNullableInt(reader, "StudentsRecruitedLastYear"),
            ExpectedStudentsNext12Months = GetNullableInt(reader, "ExpectedStudentsNext12M"),
            VisaSuccessRate = GetNullableDecimal(reader, "VisaSuccessRate"),
            ComplianceId = GetNullableInt(reader, "ComplianceId"),
            RegisteredWithRegulatoryBody = GetNullableBool(reader, "IsRegisteredWithBody") is bool registered
                ? ToYesNo(registered) : null,
            RegulatoryBodyDetails = GetNullableString(reader, "RegulatoryBodyDetails"),
            CertifiedCounselors = GetNullableBool(reader, "HasCertifiedCounselors") is bool certified
                ? ToYesNo(certified) : null,
            VisaFraudHistory = GetNullableBool(reader, "HasFraudHistory") is bool fraud
                ? ToYesNo(fraud) : null,
            VisaFraudExplanation = GetNullableString(reader, "FraudHistoryDetails"),
            ConductsSeminars = GetNullableBool(reader, "ConductsSeminars") is bool seminars
                ? ToYesNo(seminars) : null,
            PreferredPaymentTerms = GetNullableString(reader, "PreferredPaymentTerms"),
            BankingId = GetNullableInt(reader, "BankingId"),
            BankName = GetNullableString(reader, "BankName"),
            AccountName = GetNullableString(reader, "AccountName"),
            AccountNumber = GetNullableString(reader, "AccountNumber"),
            SwiftCode = GetNullableString(reader, "SwiftCode"),
            BankCountry = GetNullableString(reader, "BankCountry"),
            AuthorizedSignatoryName = GetNullableString(reader, "SignatoryName"),
            Signature = GetNullableString(reader, "SignatureUrl"),
            DeclarationDate = GetNullableDateTime(reader, "DeclaredAt")
        };

        ParseDestinationCountries(GetNullableString(reader, "DestinationCountries"), data);
        ParseMarketingChannels(GetNullableString(reader, "MarketingChannels"), data);
        ParseComplianceAgreements(reader, data);
        var visaSupport = GetNullableBool(reader, "HasVisaProcessingSupport");
        data.InHouseVisaSupport = visaSupport is bool value ? ToYesNo(value) : null;

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                var isPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary"));
                if (isPrimary)
                {
                    data.PrimaryContactName = reader.GetString(reader.GetOrdinal("ContactName"));
                    data.PrimaryContactDesignation = GetNullableString(reader, "Designation");
                    data.PrimaryContactEmail = GetNullableString(reader, "EmailAddress");
                    data.PrimaryContactMobile = GetNullableString(reader, "MobileNumber");
                }
                else
                {
                    data.SecondaryContactName = reader.GetString(reader.GetOrdinal("ContactName"));
                    data.SecondaryContactEmail = GetNullableString(reader, "EmailAddress");
                    data.SecondaryContactNumber = GetNullableString(reader, "MobileNumber");
                }
            }
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                var documentType = reader.GetString(reader.GetOrdinal("DocumentType"));
                var fileName = GetNullableString(reader, "FileName") ?? "Uploaded file";
                if (DocumentTypeToField.TryGetValue(documentType, out var fieldName))
                    data.UploadedDocuments[fieldName] = fileName;
            }
        }

        data.ResumeStep = ComputeResumeStep(data);
        return data;
    }

    private static int ComputeResumeStep(VendorOnboardingDataResponse data)
    {
        if (data.IsLinkExpired)
            return 0;

        if (string.IsNullOrWhiteSpace(data.LegalBusinessName))
            return 0;
        if (string.IsNullOrWhiteSpace(data.PrimaryContactName))
            return 1;
        if (string.IsNullOrWhiteSpace(data.BusinessType))
            return 2;
        if (string.IsNullOrWhiteSpace(data.PrimaryStudentSourceCountries))
            return 3;
        if (data.StudentsRecruitedLastYear == null)
            return 4;
        if (string.IsNullOrWhiteSpace(data.RegisteredWithRegulatoryBody))
            return 5;
        if (string.IsNullOrWhiteSpace(data.ConductsSeminars))
            return 6;
        if (string.IsNullOrWhiteSpace(data.PreferredPaymentTerms))
            return 7;

        if (string.IsNullOrWhiteSpace(data.BankName)
            || string.IsNullOrWhiteSpace(data.AccountName)
            || string.IsNullOrWhiteSpace(data.AccountNumber)
            || string.IsNullOrWhiteSpace(data.SwiftCode)
            || string.IsNullOrWhiteSpace(data.BankCountry))
            return 8;

        var requiredDocs = new[]
        {
            "companyRegistrationCertificate",
            "directorIdPassport",
            "officePhotos",
            "businessProfile"
        };

        if (requiredDocs.Any(field => !data.UploadedDocuments.ContainsKey(field)))
            return 9;

        if (string.IsNullOrWhiteSpace(data.AuthorizedSignatoryName))
            return 10;

        return 10;
    }

    private static void ParseComplianceAgreements(SqlDataReader reader, VendorOnboardingDataResponse data)
    {
        if (GetNullableBool(reader, "AgreesEthicalRecruitment") == true)
            data.ComplianceAgreements.Add("Ethical recruitment practices");
        if (GetNullableBool(reader, "AgreesDataProtection") == true)
            data.ComplianceAgreements.Add("Student data protection laws");
        if (GetNullableBool(reader, "AgreesImmigrationRegs") == true)
            data.ComplianceAgreements.Add("Immigration regulations");
    }

    private static void ParseDestinationCountries(string? raw, VendorOnboardingDataResponse data)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        foreach (var part in raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("Other:", StringComparison.OrdinalIgnoreCase))
            {
                data.DestinationCountries.Add("Other");
                data.DestinationCountriesOther = part["Other:".Length..].Trim();
            }
            else
            {
                data.DestinationCountries.Add(part);
            }
        }
    }

    private static void ParseMarketingChannels(string? raw, VendorOnboardingDataResponse data)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        foreach (var part in raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("Other:", StringComparison.OrdinalIgnoreCase))
            {
                data.MarketingChannels.Add("Other");
                data.MarketingChannelsOther = part["Other:".Length..].Trim();
            }
            else
            {
                data.MarketingChannels.Add(part);
            }
        }
    }

    private static string ToYesNo(bool value) => value ? "yes" : "no";

    private static string MapBusinessTypeToDb(string businessType)
    {
        return businessType switch
        {
            "Education Agent" => "Agent",
            "Migration Agency" => "Migration",
            _ => businessType
        };
    }

    private static string? MapBusinessTypeFromDb(string? businessType)
    {
        return businessType switch
        {
            "Agent" => "Education Agent",
            "Migration" => "Migration Agency",
            _ => businessType
        };
    }

    private static string? GetNullableString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetNullableInt(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static short? GetNullableShort(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt16(ordinal);
    }

    private static decimal? GetNullableDecimal(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    private static bool? GetNullableBool(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
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
