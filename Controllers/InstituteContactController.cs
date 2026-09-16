using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteContact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/institutes/{instituteId:int}/contact")]
[ApiController]
[Authorize]
public class InstituteContactController : ControllerBase
{
    private readonly IInstituteContactRepository _repository;
    private readonly LogHelper _logHelper;

    public InstituteContactController(IInstituteContactRepository repository, LogHelper logHelper)
    {
        _repository = repository;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetContact(int instituteId)
    {
        try
        {
            var contact = await _repository.GetByInstituteIdAsync(instituteId);
            // Return an empty shell instead of 404 so the frontend form can render
            // even before a contact record has ever been saved for this institute.
            return Ok(contact ?? InstituteContactDto.Empty(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetContact), ex);
            return StatusCode(500, "An error occurred while fetching institute contact details.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateContact(int instituteId, [FromBody] InstituteContactUpsertRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            await _repository.UpsertAsync(instituteId, request);
            return Ok(await _repository.GetByInstituteIdAsync(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateContact), ex);
            return StatusCode(500, "An error occurred while saving institute contact details.");
        }
    }
}