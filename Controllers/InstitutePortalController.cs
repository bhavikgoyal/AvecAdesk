using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/institutes/portal")]
[ApiController]
[Authorize]
public class InstitutePortalController : ControllerBase
{
    private readonly IInstitutePortalRepository _repository;
    private readonly LogHelper _logHelper;

    public InstitutePortalController(IInstitutePortalRepository repository, LogHelper logHelper)
    {
        _repository = repository;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetPortal(
        [FromQuery] string instituteName,
        [FromQuery] string? query,
        [FromQuery] string? level,
        [FromQuery] string? intake,
        [FromQuery] string? campus)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(instituteName))
                return BadRequest("instituteName is required.");

            var portal = await _repository.GetPortalByInstituteNameAsync(
                instituteName, query, level, intake, campus);

            if (portal == null)
                return NotFound($"Institute '{instituteName.Trim()}' not found.");

            return Ok(portal);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetPortal), ex);
            return StatusCode(500, "An error occurred while fetching institute portal data.");
        }
    }
}
