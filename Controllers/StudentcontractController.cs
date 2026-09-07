using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.StudentContract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/students/{studentId:int}/contracts")]
[ApiController]
[Authorize]
public class StudentContractController : ControllerBase
{
    private readonly IStudentContractRepository _repository;
    private readonly LogHelper _logHelper;

    public StudentContractController(
        IStudentContractRepository repository,
        LogHelper logHelper)
    {
        _repository = repository;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetContracts(int studentId)
    {
        try
        {
            var result = await _repository.GetByStudentIdAsync(studentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetContracts), ex);
            return StatusCode(500, "An error occurred while fetching student contracts.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateContract(int studentId, [FromBody] StudentContractUpsertRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var id = await _repository.CreateAsync(studentId, request);
            return Ok(await _repository.GetByIdAsync(studentId, id));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(CreateContract), ex);
            return StatusCode(500, "An error occurred while creating the contract.");
        }
    }

    [HttpPut("{contractId:int}")]
    public async Task<IActionResult> UpdateContract(int studentId, int contractId, [FromBody] StudentContractUpsertRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var updated = await _repository.UpdateAsync(studentId, contractId, request);
            if (!updated)
                return NotFound("Contract not found.");

            return Ok(await _repository.GetByIdAsync(studentId, contractId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateContract), ex);
            return StatusCode(500, "An error occurred while updating the contract.");
        }
    }

    [HttpDelete("{contractId:int}")]
    public async Task<IActionResult> DeleteContract(int studentId, int contractId)
    {
        try
        {
            var deleted = await _repository.DeleteAsync(studentId, contractId);
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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadContractFile(int studentId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contracts");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/contracts/{uniqueFileName}";
            return Ok(new { url = fileUrl, fileName = file.FileName });
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UploadContractFile), ex);
            return StatusCode(500, "An error occurred while uploading the file.");
        }
    }
}