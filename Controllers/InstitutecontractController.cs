using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteContract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/institutes/{instituteId:int}/contracts")]
[ApiController]
[Authorize]
public class InstituteContractController : ControllerBase
{
    private readonly IInstituteContractRepository _repository;
    private readonly LogHelper _logHelper;
    private readonly IWebHostEnvironment _env;

    public InstituteContractController(IInstituteContractRepository repository, LogHelper logHelper, IWebHostEnvironment env)
    {
        _repository = repository;
        _logHelper = logHelper;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetContracts(int instituteId)
    {
        try
        {
            return Ok(await _repository.GetByInstituteIdAsync(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetContracts), ex);
            return StatusCode(500, "An error occurred while fetching institute contracts.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateContract(int instituteId, [FromBody] InstituteContractUpsertRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var id = await _repository.CreateAsync(instituteId, request);
            return Ok(await _repository.GetByIdAsync(instituteId, id));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(CreateContract), ex);
            return StatusCode(500, "An error occurred while creating the contract.");
        }
    }

    [HttpPost("{contractId:int}")]
    public async Task<IActionResult> UpdateContract(int instituteId, int contractId, [FromBody] InstituteContractUpsertRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var updated = await _repository.UpdateAsync(instituteId, contractId, request);
            if (!updated)
                return NotFound("Contract not found.");

            return Ok(await _repository.GetByIdAsync(instituteId, contractId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateContract), ex);
            return StatusCode(500, "An error occurred while updating the contract.");
        }
    }

    [HttpDelete("{contractId:int}")]
    public async Task<IActionResult> DeleteContract(int instituteId, int contractId)
    {
        try
        {
            var deleted = await _repository.DeleteAsync(instituteId, contractId);
            if (!deleted)
                return NotFound("Contract not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(DeleteContract), ex);
            return StatusCode(500, "An error occurred while deleting the contract.");
        }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)] // 20 MB cap
    public async Task<IActionResult> UploadContractFile(int instituteId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Unsupported file type. Allowed: PDF, DOC, DOCX, JPG, PNG.");

            var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads", "contracts");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{instituteId}_{DateTime.UtcNow.Ticks}{extension}";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/contracts/{safeFileName}";
            return Ok(new { url = relativeUrl });
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UploadContractFile), ex);
            return StatusCode(500, "An error occurred while uploading the file.");
        }
    }
}