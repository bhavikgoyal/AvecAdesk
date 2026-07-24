using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/colleges")]
[ApiController]
[Authorize]
public class CollegeController : ControllerBase
{
    private readonly ICollegeRepository _repository;
    private readonly LogHelper _logHelper;

    public CollegeController(ICollegeRepository repository, LogHelper logHelper)
    {
        _repository = repository;
        _logHelper = logHelper;
    }

    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters()
    {
        try
        {
            return Ok(await _repository.GetFilterOptionsAsync());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetFilters), ex);
            return StatusCode(500, "An error occurred while fetching college filter options.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] string? campus,
        [FromQuery] string? state,
        [FromQuery] int? topCount,
        [FromQuery] bool? topCollegesOnly)
    {
        try
        {
            var colleges = await _repository.SearchCollegesAsync(query, campus, state, topCount, topCollegesOnly);
            return Ok(colleges);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(Search), ex);
            return StatusCode(500, "An error occurred while searching colleges.");
        }
    }
}
