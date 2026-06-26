using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Vendor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AvecADeskApi.Controllers;

[Authorize]
[Route("api/vendor-onboarding")]
[ApiController]
public class VendorOnboardingController : ControllerBase
{
    private readonly IVendorOnboardingRepository _repository;
    private readonly IWebHostEnvironment _environment;
    private readonly LogHelper _logHelper;

    private static readonly Dictionary<string, string> DocumentFieldTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["companyRegistrationCertificate"] = "Company Registration Certificate",
        ["directorIdPassport"] = "Director ID / Passport",
        ["officePhotos"] = "Office Photos",
        ["businessProfile"] = "Business Profile / Brochure",
        ["existingPartnerAgreements"] = "Existing Partner Agreements"
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

    [HttpPost("company-info")]
    public async Task<IActionResult> SaveCompanyInfo([FromBody] VendorCompanyInfoRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null || userId <= 0)
                return Unauthorized("User ID not found in token. Please login again.");

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
            _logHelper.LogError(nameof(SaveCompanyInfo), ex);
            return StatusCode(500, "An error occurred while saving company information.");
        }
    }

    [HttpPost("contacts")]
    public async Task<IActionResult> SaveContacts([FromBody] VendorContactsRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

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
            _logHelper.LogError(nameof(SaveContacts), ex);
            return StatusCode(500, "An error occurred while saving contact details.");
        }
    }

    [HttpPost("business-profile")]
    public async Task<IActionResult> SaveBusinessProfile([FromBody] VendorBusinessProfileRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

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
            _logHelper.LogError(nameof(SaveBusinessProfile), ex);
            return StatusCode(500, "An error occurred while saving business profile.");
        }
    }

    [HttpPost("markets")]
    public async Task<IActionResult> SaveMarkets([FromBody] VendorMarketsRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

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
            _logHelper.LogError(nameof(SaveMarkets), ex);
            return StatusCode(500, "An error occurred while saving recruitment markets.");
        }
    }

    [HttpPost("performance")]
    public async Task<IActionResult> SavePerformance([FromBody] VendorPerformanceRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

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
            _logHelper.LogError(nameof(SavePerformance), ex);
            return StatusCode(500, "An error occurred while saving performance data.");
        }
    }

    [HttpPost("compliance")]
    public async Task<IActionResult> SaveCompliance([FromBody] VendorComplianceRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

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
            _logHelper.LogError(nameof(SaveCompliance), ex);
            return StatusCode(500, "An error occurred while saving compliance details.");
        }
    }

    [HttpPost("marketing-capability")]
    public async Task<IActionResult> SaveMarketingCapability([FromBody] VendorMarketingCapabilityRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

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
            _logHelper.LogError(nameof(SaveMarketingCapability), ex);
            return StatusCode(500, "An error occurred while saving marketing capability.");
        }
    }

    [HttpPost("commercial-terms")]
    public async Task<IActionResult> SaveCommercialTerms([FromBody] VendorCommercialTermsRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            if (string.IsNullOrWhiteSpace(request.PreferredPaymentTerms))
                return BadRequest("Preferred payment terms are required.");

            var result = await _repository.SaveCommercialTermsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(SaveCommercialTerms), ex);
            return StatusCode(500, "An error occurred while saving commercial terms.");
        }
    }

    [HttpPost("banking")]
    public async Task<IActionResult> SaveBanking([FromBody] VendorBankingRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var result = await _repository.SaveBankingAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(SaveBanking), ex);
            return StatusCode(500, "An error occurred while saving banking details.");
        }
    }

    [HttpPost("documents")]
    public async Task<IActionResult> SaveDocuments([FromForm] int vendorId, [FromForm] IFormCollection form)
    {
        try
        {
            if (vendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

            var savedDocuments = new List<object>();
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "vendor-documents", vendorId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            foreach (var mapping in DocumentFieldTypes)
            {
                var file = form.Files[mapping.Key];
                if (file == null || file.Length == 0)
                {
                    if (mapping.Key != "existingPartnerAgreements")
                        return BadRequest($"{mapping.Value} is required.");
                    continue;
                }

                var extension = Path.GetExtension(file.FileName);
                if (!AllowedExtensions.Contains(extension))
                    return BadRequest($"{mapping.Value}: only PDF, JPG, and PNG files are allowed.");

                var storedFileName = $"{mapping.Key}-{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, storedFileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var documentId = await _repository.SaveDocumentAsync(vendorId, new VendorDocumentSaveRequest
                {
                    DocumentType = mapping.Value,
                    FileName = file.FileName,
                    FileUrl = filePath,
                    FileSizeKB = (int)Math.Ceiling(file.Length / 1024.0)
                });

                savedDocuments.Add(new { documentId, documentType = mapping.Value, fileName = file.FileName });
            }

            return Ok(new VendorOnboardingStepResponse
            {
                VendorId = vendorId,
                Message = "Documents saved successfully."
            });
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(SaveDocuments), ex);
            return StatusCode(500, "An error occurred while saving documents.");
        }
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitApplication([FromBody] VendorDeclarationRequest request)
    {
        try
        {
            if (request.VendorId <= 0)
                return BadRequest("Vendor ID is required. Please complete company information first.");

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
            _logHelper.LogError(nameof(SubmitApplication), ex);
            return StatusCode(500, "An error occurred while submitting the application.");
        }
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;

        return null;
    }
}
