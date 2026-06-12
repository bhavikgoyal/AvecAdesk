using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AvecADeskApi.Controllers;

[Route("api/uploads")]
[ApiController]
[Authorize]
public class UploadsController : ControllerBase
{
    private readonly IUploadRepository _uploadRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly LogHelper _logHelper;

    public UploadsController(IUploadRepository uploadRepository, IWebHostEnvironment environment, LogHelper logHelper)
    {
        _uploadRepository = uploadRepository;
        _environment = environment;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetUploads([FromQuery] int? instituteId)
    {
        try
        {
            return Ok(await _uploadRepository.GetUploadsAsync(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetUploads), ex);
            return StatusCode(500, "An error occurred while fetching uploads.");
        }
    }

    [HttpPost("institute")]
    public async Task<IActionResult> UploadInstituteExcel(IFormFile file, [FromForm] int instituteId)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("Excel file is required");

            var userId = GetCurrentUserId() ?? 15;

            var folder = Path.Combine(_environment.ContentRootPath, "uploads", "institute-excel", instituteId.ToString());
            Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var uploadId = await _uploadRepository.UploadInstituteExcelAsync(instituteId, userId, filePath);
            return Ok(await _uploadRepository.GetUploadByIdAsync(uploadId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UploadInstituteExcel), ex);
            return StatusCode(500, "An error occurred while uploading the Excel file.");
        }
    }

    [HttpGet("{uploadId:int}/diff")]
    public async Task<IActionResult> GetUploadDiff(int uploadId)
    {
        try
        {
            var diff = await _uploadRepository.GetUploadDiffAsync(uploadId);
            if (diff == null) return NotFound("Upload not found");
            return Ok(diff);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetUploadDiff), ex);
            return StatusCode(500, "An error occurred while fetching upload diff.");
        }
    }

    [HttpPost("{uploadId:int}/reconcile")]
    public async Task<IActionResult> ReconcileUpload(int uploadId)
    {
        try
        {
            var ok = await _uploadRepository.ReconcileUploadAsync(uploadId);
            if (!ok) return NotFound("Upload not found");
            return Ok(await _uploadRepository.GetUploadByIdAsync(uploadId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(ReconcileUpload), ex);
            return StatusCode(500, "An error occurred while reconciling upload.");
        }
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
