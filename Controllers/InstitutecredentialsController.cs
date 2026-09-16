using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteCredential;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/institutes/{instituteId:int}/credentials")]
[ApiController]
[Authorize(Roles = "Accounting,Admin,Super Admin")]
public class InstituteCredentialsController : ControllerBase
{
    private readonly IInstituteCredentialRepository _repository;
    private readonly LogHelper _logHelper;

    public InstituteCredentialsController(IInstituteCredentialRepository repository, LogHelper logHelper)
    {
        _repository = repository;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetCredentials(int instituteId)
    {
        try
        {
            return Ok(await _repository.GetByInstituteIdAsync(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetCredentials), ex);
            return StatusCode(500, "An error occurred while fetching institute credentials.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCredential(int instituteId, [FromBody] InstituteCredentialUpsertRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Url) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Name, URL, username and password are all required.");

            var credentialId = await _repository.CreateAsync(instituteId, request);
            return Ok(await _repository.GetByIdAsync(instituteId, credentialId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(CreateCredential), ex);
            return StatusCode(500, "An error occurred while creating the credential.");
        }
    }

    [HttpPost("{credentialId:int}")]
    public async Task<IActionResult> UpdateCredential(int instituteId, int credentialId, [FromBody] InstituteCredentialUpsertRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var updated = await _repository.UpdateAsync(instituteId, credentialId, request);
            if (!updated)
                return NotFound("Credential not found.");

            return Ok(await _repository.GetByIdAsync(instituteId, credentialId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateCredential), ex);
            return StatusCode(500, "An error occurred while updating the credential.");
        }
    }

    [HttpDelete("{credentialId:int}")]
    public async Task<IActionResult> DeleteCredential(int instituteId, int credentialId)
    {
        try
        {
            var deleted = await _repository.DeleteAsync(instituteId, credentialId);
            if (!deleted)
                return NotFound("Credential not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(DeleteCredential), ex);
            return StatusCode(500, "An error occurred while deleting the credential.");
        }
    }
}