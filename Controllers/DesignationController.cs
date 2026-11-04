using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/designations")]
[ApiController]
[Authorize]
public class DesignationController : ControllerBase
{
    private readonly IDesignationRepository _repository;
    private readonly LogHelper _logHelper;

    public DesignationController(IDesignationRepository repository, LogHelper logHelper)
    {
        _repository = repository;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetDesignations()
    {
        try
        {
            return Ok(await _repository.GetAllAsync());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetDesignations), ex);
            return StatusCode(500, "An error occurred while fetching designations.");
        }
    }
}
