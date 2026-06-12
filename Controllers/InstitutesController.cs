using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Institute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/institutes")]
[ApiController]
public class InstitutesController : ControllerBase
{
    private readonly IInstituteRepository _instituteRepository;
    private readonly LogHelper _logHelper;

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Activate", "Suspend", "Active", "Inactive", "Suspended"
    };

    public InstitutesController(IInstituteRepository instituteRepository, LogHelper logHelper)
    {
        _instituteRepository = instituteRepository;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> SearchInstitutes(
        [FromQuery] string? name,
        [FromQuery] string? city,
        [FromQuery] string? service)
    {
        try
        {
            return Ok(await _instituteRepository.SearchInstitutesAsync(
                NormalizeSearchParam(name),
                NormalizeSearchParam(city),
                NormalizeSearchParam(service)));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(SearchInstitutes), ex);
            return StatusCode(500, "An error occurred while searching institutes.");
        }
    }

    [HttpGet("admin")]
    [Authorize]
    public async Task<IActionResult> GetInstitutesAdmin([FromQuery] string? status)
    {
        try
        {
            return Ok(await _instituteRepository.GetInstitutesAdminAsync(status));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetInstitutesAdmin), ex);
            return StatusCode(500, "An error occurred while fetching institutes.");
        }
    }

    [HttpGet("{instituteId:int}")]
    public async Task<IActionResult> GetInstituteById(int instituteId)
    {
        try
        {
            var institute = await _instituteRepository.GetInstituteByIdAsync(instituteId);
            if (institute == null)
                return NotFound("Institute not found");

            return Ok(institute);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetInstituteById), ex);
            return StatusCode(500, "An error occurred while fetching the institute.");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateInstitute([FromBody] InstituteCreateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InstituteName))
                return BadRequest("Institute name is required");

            if (request.VendorId <= 0)
                return BadRequest("Valid vendor ID is required");

            var instituteId = await _instituteRepository.CreateInstituteAsync(request);
            var created = await _instituteRepository.GetInstituteByIdAsync(instituteId);
            return Ok(created);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(CreateInstitute), ex);
            return StatusCode(500, "An error occurred while creating the institute.");
        }
    }

    [HttpPut("{instituteId:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateInstitute(int instituteId, [FromBody] InstituteUpdateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InstituteName))
                return BadRequest("Institute name is required");

            var updated = await _instituteRepository.UpdateInstituteAsync(instituteId, request);
            if (!updated)
                return NotFound("Institute not found");

            return Ok(await _instituteRepository.GetInstituteByIdAsync(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateInstitute), ex);
            return StatusCode(500, "An error occurred while updating the institute.");
        }
    }

    [HttpPut("{instituteId:int}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateInstituteStatus(int instituteId, [FromBody] InstituteStatusRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Status) || !AllowedStatuses.Contains(request.Status))
                return BadRequest("Invalid status. Use Activate or Suspend.");

            var dbStatus = MapStatusToDatabase(request.Status);
            var updated = await _instituteRepository.UpdateInstituteStatusAsync(instituteId, dbStatus);
            if (!updated)
                return NotFound("Institute not found");

            return Ok(await _instituteRepository.GetInstituteByIdAsync(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateInstituteStatus), ex);
            return StatusCode(500, "An error occurred while updating institute status.");
        }
    }

    [HttpPut("{instituteId:int}/publish")]
    [Authorize]
    public async Task<IActionResult> PublishInstitute(int instituteId)
    {
        try
        {
            var published = await _instituteRepository.PublishInstituteAsync(instituteId);
            if (!published)
                return NotFound("Institute not found");

            return Ok(await _instituteRepository.GetInstituteByIdAsync(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(PublishInstitute), ex);
            return StatusCode(500, "An error occurred while publishing the institute.");
        }
    }

    private static string? NormalizeSearchParam(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private static string MapStatusToDatabase(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "activate" => "Active",
            "suspend" => "Suspended",
            _ => status
        };
    }
}
