using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteContact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/institutes/{instituteId:int}/contacts")]
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
    public async Task<IActionResult> GetContacts(int instituteId)
    {
        try
        {
            return Ok(await _repository.GetByInstituteIdAsync(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetContacts), ex);
            return StatusCode(500, "An error occurred while fetching institute contacts.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateContact(int instituteId, [FromBody] InstituteContactUpsertRequest request)
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
            _logHelper.LogError(nameof(CreateContact), ex);
            return StatusCode(500, "An error occurred while creating the contact.");
        }
    }

    [HttpPut("{contactId:int}")]
    public async Task<IActionResult> UpdateContact(int instituteId, int contactId, [FromBody] InstituteContactUpsertRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var updated = await _repository.UpdateAsync(instituteId, contactId, request);
            if (!updated)
                return NotFound("Contact not found.");

            return Ok(await _repository.GetByIdAsync(instituteId, contactId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateContact), ex);
            return StatusCode(500, "An error occurred while updating the contact.");
        }
    }

    [HttpDelete("{contactId:int}")]
    public async Task<IActionResult> DeleteContact(int instituteId, int contactId)
    {
        try
        {
            var deleted = await _repository.DeleteAsync(instituteId, contactId);
            if (!deleted)
                return NotFound("Contact not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(DeleteContact), ex);
            return StatusCode(500, "An error occurred while deleting the contact.");
        }
    }
}