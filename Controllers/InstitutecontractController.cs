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

    public InstituteContractController(IInstituteContractRepository repository, LogHelper logHelper)
    {
        _repository = repository;
        _logHelper = logHelper;
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

    [HttpPut("{contractId:int}")]
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
}