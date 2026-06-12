using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Vendor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AvecADeskApi.Controllers;

[Route("api/vendors")]
[ApiController]
public class VendorsController : ControllerBase
{
    private readonly IVendorRepository _vendorRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly LogHelper _logHelper;

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Activate", "Disable", "Suspend", "Active", "Disabled", "Suspended", "Pending"
    };

    private static readonly Dictionary<string, string> AllowedAgreementTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Initial"] = "Initial",
        ["Renewal"] = "Renewal",
        ["Amendment"] = "Amendment"
    };

    public VendorsController(
        IVendorRepository vendorRepository,
        IWebHostEnvironment environment,
        LogHelper logHelper)
    {
        _vendorRepository = vendorRepository;
        _environment = environment;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetVendors([FromQuery] string? status)
    {
        try
        {
            return Ok(await _vendorRepository.GetVendorsAsync(status));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetVendors), ex);
            return StatusCode(500, "An error occurred while fetching vendors.");
        }
    }

    [HttpGet("{vendorId:int}")]
    public async Task<IActionResult> GetVendorById(int vendorId)
    {
        try
        {
            var vendor = await _vendorRepository.GetVendorByIdAsync(vendorId);
            if (vendor == null)
                return NotFound("Vendor not found");

            return Ok(vendor);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetVendorById), ex);
            return StatusCode(500, "An error occurred while fetching the vendor.");
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterVendor([FromBody] VendorRegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BusinessName))
                return BadRequest("Business name is required");

            if (string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest("Phone is required");

            var vendorId = await _vendorRepository.RegisterVendorAsync(request);
            var created = await _vendorRepository.GetVendorByIdAsync(vendorId);
            return Ok(created);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(RegisterVendor), ex);
            return StatusCode(500, "An error occurred while registering the vendor.");
        }
    }

    [HttpPut("{vendorId:int}")]
    public async Task<IActionResult> UpdateVendor(int vendorId, [FromBody] VendorUpdateRequest request)
    {
        try
        {
            var updated = await _vendorRepository.UpdateVendorAsync(vendorId, request);
            if (!updated)
                return NotFound("Vendor not found");

            return Ok(await _vendorRepository.GetVendorByIdAsync(vendorId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateVendor), ex);
            return StatusCode(500, "An error occurred while updating the vendor.");
        }
    }

    [HttpPut("{vendorId:int}/status")]
    public async Task<IActionResult> UpdateVendorStatus(int vendorId, [FromBody] VendorStatusRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Status) || !AllowedStatuses.Contains(request.Status))
                return BadRequest("Invalid status. Use Activate, Disable, or Suspend.");

            var dbStatus = MapStatusToDatabase(request.Status);
            var updated = await _vendorRepository.UpdateVendorStatusAsync(vendorId, dbStatus);
            if (!updated)
                return NotFound("Vendor not found");

            return Ok(await _vendorRepository.GetVendorByIdAsync(vendorId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateVendorStatus), ex);
            return StatusCode(500, "An error occurred while updating vendor status.");
        }
    }

    [Authorize]
    [HttpPut("{vendorId:int}/approve")]
    public async Task<IActionResult> ApproveVendor(int vendorId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token. Please login again to get a new token.");

            var vendor = await _vendorRepository.ApproveVendorAsync(vendorId, userId);
            if (vendor == null)
                return NotFound("Vendor not found or cannot be approved");

            return Ok(vendor);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(ApproveVendor), ex);
            return StatusCode(500, "An error occurred while approving the vendor.");
        }
    }

    [Authorize]
    [HttpGet("{vendorId:int}/agreement")]
    public async Task<IActionResult> GetVendorAgreement(int vendorId)
    {
        try
        {
            var agreement = await _vendorRepository.GetVendorAgreementAsync(vendorId);
            if (agreement == null)
                return NotFound("Agreement not found");

            if (!System.IO.File.Exists(agreement.AgreementPath))
                return Ok(agreement);

            var fileName = Path.GetFileName(agreement.AgreementPath);
            var fileBytes = await System.IO.File.ReadAllBytesAsync(agreement.AgreementPath);
            return File(fileBytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetVendorAgreement), ex);
            return StatusCode(500, "An error occurred while fetching the agreement.");
        }
    }

    [Authorize]
    [HttpPost("{vendorId:int}/agreement")]
    public async Task<IActionResult> UploadVendorAgreement(
        int vendorId,
        IFormFile file,
        [FromForm] VendorAgreementUploadRequest request)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("Agreement file is required");

            if (!TryNormalizeAgreementType(request.AgreementType, out var agreementType))
                return BadRequest("Invalid AgreementType. Allowed values: Initial, Renewal, Amendment.");

            request.AgreementType = agreementType;

            var vendor = await _vendorRepository.GetVendorByIdAsync(vendorId);
            if (vendor == null)
                return NotFound("Vendor not found");

            var uploadedByUserId = 15;//GetCurrentUserId();
            if (uploadedByUserId == null)
                return Unauthorized("User ID not found in token. Please login again to get a new token.");

            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "agreements", vendorId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var agreementId = await _vendorRepository.UploadVendorAgreementAsync(
                vendorId, request, filePath, uploadedByUserId);

            var response = await _vendorRepository.GetVendorAgreementAsync(vendorId);
            if (response != null)
            {
                response.AgreementId = agreementId;
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UploadVendorAgreement), ex);
            return StatusCode(500, "An error occurred while uploading the agreement.");
        }
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;

        return null;
    }

    private static bool TryNormalizeAgreementType(string? agreementType, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(agreementType))
        {
            normalized = "Initial";
            return true;
        }

        return AllowedAgreementTypes.TryGetValue(agreementType.Trim(), out normalized!);
    }

    private static string MapStatusToDatabase(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "activate" => "Active",
            "disable" => "Disabled",
            "suspend" => "Suspended",
            _ => status
        };
    }
}
