using AvecADeskApi.Helper;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AvecADeskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentApplicationController : ControllerBase
{
    private readonly IStudentApplicationRepository _repo;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly IWebHostEnvironment _env;

    public StudentApplicationController(IStudentApplicationRepository repo, JwtTokenGenerator tokenGenerator, IWebHostEnvironment env)
    {
        _repo = repo;
        _tokenGenerator = tokenGenerator;
        _env = env;
    }

    // GET: api/StudentApplication/GetStudentApplicationList
    [HttpGet("GetStudentApplicationList")]
    public async Task<IActionResult> GetStudentApplications(
        [FromQuery] string? search,
        [FromQuery] int pagenumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? vendorId = null)
    {
        if (vendorId is null or <= 0)
        {
            var vendorIdClaim = User.FindFirst("vendorId")?.Value ?? User.FindFirst("VendorId")?.Value;
            if (int.TryParse(vendorIdClaim, out var claimVendorId) && claimVendorId > 0)
            {
                vendorId = claimVendorId;
            }
            else
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var userId) && userId > 0)
                {
                    vendorId = await _repo.GetVendorIdByUserIdAsync(userId);
                }
            }
        }

        // Never return unfiltered list for vendor portal history.
        if (vendorId is null or <= 0)
        {
            return Ok(new
            {
                Data = Array.Empty<StudentApplicationDetailsModel>(),
                TotalRecords = 0,
                PageNumber = pagenumber,
                PageSize = pageSize,
                Message = "VendorId is required."
            });
        }

        var result = await _repo.GetStudentApplicationsAsync(search, pagenumber, pageSize, vendorId);
        int totalRecords = result.Count > 0 ? result[0].TotalRecords : 0;
        return Ok(new
        {
            Data = result,
            TotalRecords = totalRecords,
            PageNumber = pagenumber,
            PageSize = pageSize
        });
    }

    // GET: api/StudentApplication/my-vendor-id
    [HttpGet("my-vendor-id")]
    public async Task<IActionResult> GetMyVendorId()
    {
        // Prefer VendorId claim from login token (correct vendor when UserId is shared)
        var vendorIdClaim = User.FindFirst("vendorId")?.Value ?? User.FindFirst("VendorId")?.Value;
        if (int.TryParse(vendorIdClaim, out var claimVendorId) && claimVendorId > 0)
            return Ok(new { VendorId = claimVendorId });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            return Unauthorized(new { Message = "Vendor token required." });

        var vendorId = await _repo.GetVendorIdByUserIdAsync(userId);
        if (vendorId is null or <= 0)
            return NotFound(new { Message = "Vendor not found for this user." });

        return Ok(new { VendorId = vendorId });
    }

    // GET: api/StudentApplication/vendor/{vendorId}/history
    [HttpGet("vendor/{vendorId:int}/history")]
    public async Task<IActionResult> GetVendorApplicationHistory(
        int vendorId,
        [FromQuery] string? search,
        [FromQuery] int pagenumber = 1,
        [FromQuery] int pageSize = 100)
    {
        if (vendorId <= 0)
            return BadRequest(new { Message = "Valid VendorId is required." });

        var result = await _repo.GetStudentApplicationsAsync(search, pagenumber, pageSize, vendorId);
        int totalRecords = result.Count > 0 ? result[0].TotalRecords : 0;
        return Ok(new
        {
            Data = result,
            TotalRecords = totalRecords,
            PageNumber = pagenumber,
            PageSize = pageSize
        });
    }

    // GET: api/StudentApplication/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _repo.GetApplicationByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // POST: api/StudentApplication
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StudentApplicationCreateRequest request)
    {
        var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(studentIdClaim) || !Guid.TryParse(studentIdClaim, out var studentId))
        {
            return Unauthorized(new { Message = "Invalid or missing student token." });
        }

        var applicationId = await _repo.CreateApplicationAsync(studentId, request);
        return Ok(new { ApplicationId = applicationId });
    }

    // PUT: api/StudentApplication/{id}/details
    [HttpPut("{id}/details")]
    public async Task<IActionResult> SaveDetails(Guid id, [FromBody] ApplicationDetailRequest request)
    {
        if (request.VendorId is null or <= 0)
        {
            var vendorIdClaim = User.FindFirst("vendorId")?.Value ?? User.FindFirst("VendorId")?.Value;
            if (int.TryParse(vendorIdClaim, out var claimVendorId) && claimVendorId > 0)
            {
                request.VendorId = claimVendorId;
            }
            else
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var userId) && userId > 0)
                {
                    request.VendorId = await _repo.GetVendorIdByUserIdAsync(userId);
                }
            }
        }

        var result = await _repo.SaveApplicationDetailAsync(id, request);

        if (result == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        var token = _tokenGenerator.StudentGenerateToken(result.Id, result.UserName!);

        return Ok(new
        {
            Message = "Details saved successfully.",
            Token = token,
            Data = result
        });
    }

    // POST: api/StudentApplication/{id}/documents
    [HttpPost("{id}/documents")]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] ApplicationDocumentRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { Message = "File is required" });


        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", id.ToString());
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await request.File.CopyToAsync(stream);

        var fileUrl = $"/uploads/{id}/{fileName}";
        var result = await _repo.UploadDocumentAsync(id, request, fileUrl);
        return Ok(result);
    }

    // DELETE: api/StudentApplication/documents/{docId}
    [HttpDelete("documents/{docId}")]
    public async Task<IActionResult> DeleteDocument(Guid docId)
    {
        var result = await _repo.DeleteDocumentAsync(docId);
        if (!result) return NotFound();
        return Ok(new { Message = "Document deleted successfully" });
    }

    // PUT: api/StudentApplication/{id}/declaration
    [HttpPut("{id}/declaration")]
    public async Task<IActionResult> SaveDeclaration(Guid id, [FromBody] DeclarationRequest request)
    {
        var result = await _repo.SaveDeclarationAsync(id, request);
        if (!result) return NotFound();
        return Ok(new { Message = "Declaration saved successfully" });
    }

    // POST: api/StudentApplication/{id}/submit
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        var result = await _repo.SubmitApplicationAsync(id);
        if (!result) return NotFound();
        return Ok(new { Message = "Application submitted successfully" });
    }


}