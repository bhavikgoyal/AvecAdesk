using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Vendor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace AvecADeskApi.Controllers;

[AllowAnonymous]
[Route("api/vendor-onboarding")]
[ApiController]
public class VendorOnboardingController : ControllerBase
{
    private readonly IVendorOnboardingRepository _repository;
    private readonly IWebHostEnvironment _environment;
    private readonly LogHelper _logHelper;

    private static readonly Dictionary<string, (string DbType, string Label)> DocumentFieldTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["companyRegistrationCertificate"] = ("RegCert", "Company Registration Certificate"),
        ["directorIdPassport"] = ("DirectorID", "Director ID / Passport"),
        ["officePhotos"] = ("OfficePhoto", "Office Photos"),
        ["businessProfile"] = ("Brochure", "Business Profile / Brochure"),
        ["existingPartnerAgreements"] = ("PartnerAgreement", "Existing Partner Agreements"),
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    public VendorOnboardingController(
        IVendorOnboardingRepository repository,
        IWebHostEnvironment environment,
        LogHelper logHelper)
    {
        _repository = repository;
        _environment = environment;
        _logHelper = logHelper;
    }

    [HttpGet("{vendorId:int}")]
    public async Task<IActionResult> GetOnboarding(int vendorId)
    {
        try
        {
            var data = await _repository.GetOnboardingAsync(vendorId);
            if (data == null)
                return NotFound("Vendor onboarding record not found.");

            return Ok(data);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(GetOnboarding), "An error occurred while loading onboarding data.");
        }
    }

    [HttpPost("company-info")]
    public async Task<IActionResult> SaveCompanyInfo([FromBody] VendorCompanyInfoRequest request)
    {
        try
        {
            if (request.VendorId is > 0)
            {
                var expired = await RejectIfLinkExpiredAsync(request.VendorId.Value);
                if (expired != null)
                    return expired;
            }

            var userId = request.VendorId is > 0
                ? await _repository.GetVendorUserIdAsync(request.VendorId.Value)
                : null;
            userId ??= GetCurrentUserId();

            if (userId == null || userId <= 0)
                return Unauthorized("User ID not found. Please use your onboarding email link or login again.");

            if (string.IsNullOrWhiteSpace(request.LegalBusinessName))
                return BadRequest("Legal business name is required.");

            if (string.IsNullOrWhiteSpace(request.CompanyRegistrationNumber))
                return BadRequest("Company registration number is required.");

            if (string.IsNullOrWhiteSpace(request.CountryOfRegistration))
                return BadRequest("Country of registration is required.");

            if (string.IsNullOrWhiteSpace(request.RegisteredOfficeAddress))
                return BadRequest("Registered office address is required.");

            var result = await _repository.SaveCompanyInfoAsync(userId.Value, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SaveCompanyInfo), "An error occurred while saving company information.");
        }
    }

    [HttpPost("contacts")]
    public async Task<IActionResult> SaveContacts([FromBody] VendorContactsRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var expired = await RejectIfLinkExpiredAsync(request.VendorId);
            if (expired != null)
                return expired;

            if (string.IsNullOrWhiteSpace(request.PrimaryContactName))
                return BadRequest("Primary contact name is required.");

            if (string.IsNullOrWhiteSpace(request.PrimaryContactEmail))
                return BadRequest("Primary contact email is required.");

            if (string.IsNullOrWhiteSpace(request.PrimaryContactMobile))
                return BadRequest("Primary contact mobile number is required.");

            var result = await _repository.SaveContactsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SaveContacts), "An error occurred while saving contact details.");
        }
    }

    [HttpPost("business-profile")]
    public async Task<IActionResult> SaveBusinessProfile([FromBody] VendorBusinessProfileRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var expired = await RejectIfLinkExpiredAsync(request.VendorId);
            if (expired != null)
                return expired;

            if (string.IsNullOrWhiteSpace(request.BusinessType))
                return BadRequest("Type of business is required.");

            if (request.NumberOfEmployees == null || request.NumberOfEmployees < 0)
                return BadRequest("Number of employees is required.");

            if (request.NumberOfCounselors == null || request.NumberOfCounselors < 0)
                return BadRequest("Number of counselors is required.");

            if (request.NumberOfOffices == null || request.NumberOfOffices < 0)
                return BadRequest("Number of offices is required.");

            if (request.YearsOfExperience == null || request.YearsOfExperience < 0)
                return BadRequest("Years of experience is required.");

            if (string.Equals(request.BusinessType, "Other", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(request.BusinessTypeOther))
            {
                return BadRequest("Please specify the business type.");
            }

            var result = await _repository.SaveBusinessProfileAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SaveBusinessProfile), "An error occurred while saving business profile.");
        }
    }

    [HttpPost("markets")]
    public async Task<IActionResult> SaveMarkets([FromBody] VendorMarketsRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var expired = await RejectIfLinkExpiredAsync(request.VendorId);
            if (expired != null)
                return expired;

            if (string.IsNullOrWhiteSpace(request.PrimaryStudentSourceCountries))
                return BadRequest("Primary student source countries are required.");

            if (string.IsNullOrWhiteSpace(request.Top5Institutions))
                return BadRequest("Top 5 institutions are required.");

            if (request.DestinationCountries == null || request.DestinationCountries.Count == 0)
                return BadRequest("Please select at least one destination country.");

            var result = await _repository.SaveMarketsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SaveMarkets), "An error occurred while saving recruitment markets.");
        }
    }

    [HttpPost("performance")]
    public async Task<IActionResult> SavePerformance([FromBody] VendorPerformanceRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var expired = await RejectIfLinkExpiredAsync(request.VendorId);
            if (expired != null)
                return expired;

            if (request.StudentsRecruitedLastYear == null || request.StudentsRecruitedLastYear < 0)
                return BadRequest("Students recruited last year is required.");

            if (request.ExpectedStudentsNext12Months == null || request.ExpectedStudentsNext12Months < 0)
                return BadRequest("Expected students for next 12 months is required.");

            if (request.VisaSuccessRate == null || request.VisaSuccessRate < 0 || request.VisaSuccessRate > 100)
                return BadRequest("Visa success rate must be between 0 and 100.");

            var result = await _repository.SavePerformanceAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SavePerformance), "An error occurred while saving performance data.");
        }
    }

    [HttpPost("compliance")]
    public async Task<IActionResult> SaveCompliance([FromBody] VendorComplianceRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var expired = await RejectIfLinkExpiredAsync(request.VendorId);
            if (expired != null)
                return expired;

            if (string.IsNullOrWhiteSpace(request.RegisteredWithRegulatoryBody))
                return BadRequest("Regulatory registration answer is required.");

            if (string.Equals(request.RegisteredWithRegulatoryBody, "yes", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(request.RegulatoryBodyDetails))
            {
                return BadRequest("Please provide regulatory body details.");
            }

            if (string.IsNullOrWhiteSpace(request.CertifiedCounselors))
                return BadRequest("Certified counselors answer is required.");

            if (string.IsNullOrWhiteSpace(request.VisaFraudHistory))
                return BadRequest("Visa fraud history answer is required.");

            if (string.Equals(request.VisaFraudHistory, "yes", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(request.VisaFraudExplanation))
            {
                return BadRequest("Please explain the visa fraud history.");
            }

            if (request.ComplianceAgreements == null || request.ComplianceAgreements.Count == 0)
                return BadRequest("Please select at least one compliance agreement.");

            var result = await _repository.SaveComplianceAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SaveCompliance), "An error occurred while saving compliance details.");
        }
    }

    [HttpPost("marketing-capability")]
    public async Task<IActionResult> SaveMarketingCapability([FromBody] VendorMarketingCapabilityRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var expired = await RejectIfLinkExpiredAsync(request.VendorId);
            if (expired != null)
                return expired;

            if (string.IsNullOrWhiteSpace(request.ConductsSeminars))
                return BadRequest("Seminars/events answer is required.");

            if (request.MarketingChannels == null || request.MarketingChannels.Count == 0)
                return BadRequest("Please select at least one marketing channel.");

            if (string.IsNullOrWhiteSpace(request.InHouseVisaSupport))
                return BadRequest("In-house visa support answer is required.");

            var result = await _repository.SaveMarketingCapabilityAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SaveMarketingCapability), "An error occurred while saving marketing capability.");
        }
    }

    [HttpPost("commercial-terms")]
    public async Task<IActionResult> SaveCommercialTerms([FromBody] VendorCommercialTermsRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var expired = await RejectIfLinkExpiredAsync(request.VendorId);
            if (expired != null)
                return expired;

            if (string.IsNullOrWhiteSpace(request.PreferredPaymentTerms))
                return BadRequest("Preferred payment terms are required.");

            var result = await _repository.SaveCommercialTermsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SaveCommercialTerms), "An error occurred while saving commercial terms.");
        }
    }

    [HttpPost("banking")]
    public async Task<IActionResult> SaveBanking([FromBody] VendorBankingRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var expired = await RejectIfLinkExpiredAsync(request.VendorId);
            if (expired != null)
                return expired;

            if (string.IsNullOrWhiteSpace(request.BankName))
                return BadRequest("Bank name is required.");

            if (string.IsNullOrWhiteSpace(request.AccountName))
                return BadRequest("Account name is required.");

            if (string.IsNullOrWhiteSpace(request.AccountNumber))
                return BadRequest("Account number is required.");

            if (string.IsNullOrWhiteSpace(request.SwiftCode))
                return BadRequest("SWIFT code is required.");

            if (string.IsNullOrWhiteSpace(request.BankCountry))
                return BadRequest("Country is required.");

            var result = await _repository.SaveBankingAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SaveBanking), "An error occurred while saving banking details.");
        }
    }

    [HttpPost("documents")]
    public async Task<IActionResult> SaveDocuments([FromForm] int vendorId, [FromForm] IFormCollection form)
    {
        try
        {
            if (vendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var expired = await RejectIfLinkExpiredAsync(vendorId);
            if (expired != null)
                return expired;

            var existing = await _repository.GetOnboardingAsync(vendorId);
            var uploadedDocuments = existing?.UploadedDocuments ?? new Dictionary<string, string>();

            var savedDocuments = new List<object>();
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "vendor-documents", vendorId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            foreach (var mapping in DocumentFieldTypes)
            {
                var file = form.Files[mapping.Key];
                if (file == null || file.Length == 0)
                {
                    if (mapping.Key != "existingPartnerAgreements" && !uploadedDocuments.ContainsKey(mapping.Key))
                        return BadRequest($"{mapping.Value.Label} is required.");
                    continue;
                }

                var extension = Path.GetExtension(file.FileName);
                if (!AllowedExtensions.Contains(extension))
                    return BadRequest($"{mapping.Value.Label}: only PDF, JPG, and PNG files are allowed.");

                var storedFileName = $"{mapping.Key}-{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, storedFileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var documentId = await _repository.SaveDocumentAsync(vendorId, new VendorDocumentSaveRequest
                {
                    DocumentType = mapping.Value.DbType,
                    FileName = file.FileName,
                    FileUrl = filePath,
                    FileSizeKB = (int)Math.Ceiling(file.Length / 1024.0)
                });

                savedDocuments.Add(new { documentId, documentType = mapping.Value.DbType, fileName = file.FileName });
            }

            return Ok(new VendorOnboardingStepResponse
            {
                VendorId = vendorId,
                Message = "Documents saved successfully."
            });
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SaveDocuments), "An error occurred while saving documents.");
        }
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitApplication([FromBody] VendorDeclarationRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var expired = await RejectIfLinkExpiredAsync(request.VendorId);
            if (expired != null)
                return expired;

            if (string.IsNullOrWhiteSpace(request.AuthorizedSignatoryName))
                return BadRequest("Authorized signatory name is required.");

            if (string.IsNullOrWhiteSpace(request.Signature))
                return BadRequest("Signature is required.");

            if (request.DeclarationDate == null)
                return BadRequest("Declaration date is required.");

            var agreements = request.DeclarationItems ?? new List<string>();
            if (!agreements.Contains("All information provided is true and correct")
                || !agreements.Contains("We agree to comply with all applicable laws and partner requirements")
                || !agreements.Contains("We understand that approval is subject to due diligence"))
            {
                return BadRequest("Please confirm all declaration items.");
            }

            var result = await _repository.SubmitDeclarationAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleOnboardingError(ex, nameof(SubmitApplication), "An error occurred while submitting the application.");
        }
    }

    private async Task<IActionResult?> RejectIfLinkExpiredAsync(int vendorId)
    {
        if (await _repository.IsOnboardingLinkExpiredAsync(vendorId))
        {
            return BadRequest("This onboarding link has expired. Your application has already been submitted.");
        }

        return null;
    }

    private IActionResult HandleOnboardingError(Exception ex, string actionName, string fallbackMessage)
    {
        _logHelper.LogError(actionName, ex);

        var message = ExtractUserFacingError(ex);
        if (!string.IsNullOrWhiteSpace(message))
            return BadRequest(message);

        return StatusCode(500, fallbackMessage);
    }

    private static string? ExtractUserFacingError(Exception ex)
    {
        if (ex is SqlException sqlEx)
            return FormatSqlException(sqlEx);

        if (ex.InnerException is SqlException innerSqlEx)
            return FormatSqlException(innerSqlEx);

        return null;
    }

    private static string FormatSqlException(SqlException sqlEx)
    {
        if (sqlEx.Number == 547
            && sqlEx.Message.Contains("CK_VB_BusinessType", StringComparison.OrdinalIgnoreCase))
        {
            return "Invalid business type selected. Please choose Education Agent, Migration Agency, Both, or Other.";
        }

        var message = sqlEx.Message.Trim();
        var procedureMarker = "Procedure ";
        var procedureIndex = message.IndexOf(procedureMarker, StringComparison.OrdinalIgnoreCase);
        if (procedureIndex > 0)
            message = message[..procedureIndex].Trim();

        return message;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;

        return null;
    }
}
