using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Aih;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/aih")]
[ApiController]
public class AihController : ControllerBase
{
    private readonly IAihRepository _aihRepository;
    private readonly LogHelper _logHelper;

    public AihController(IAihRepository aihRepository, LogHelper logHelper)
    {
        _aihRepository = aihRepository;
        _logHelper = logHelper;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitAihForm([FromBody] AihSubmitRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest("Phone is required");

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required");

            if (request.InstituteId <= 0)
                return BadRequest("Valid institute ID is required");

            var interestId = await _aihRepository.SubmitAihFormAsync(request);
            var interests = await _aihRepository.GetAihInterestsAsync(null, request.InstituteId);
            var created = interests.FirstOrDefault(x => x.InterestId == interestId);

            return Ok(created ?? new AihResponse { InterestId = interestId });
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(SubmitAihForm), ex);
            return StatusCode(500, "An error occurred while submitting the AIH form.");
        }
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAihInterests([FromQuery] int? vendorId, [FromQuery] int? instituteId)
    {
        try
        {
            return Ok(await _aihRepository.GetAihInterestsAsync(vendorId, instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetAihInterests), ex);
            return StatusCode(500, "An error occurred while fetching AIH interests.");
        }
    }

    [Authorize]
    [HttpPut("{interestId:int}/convert")]
    public async Task<IActionResult> ConvertAihToStudent(int interestId)
    {
        try
        {
            var result = await _aihRepository.ConvertAihToStudentAsync(interestId);
            if (result == null)
                return NotFound("Interest not found or already converted");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(ConvertAihToStudent), ex);
            return StatusCode(500, "An error occurred while converting AIH interest to student.");
        }
    }
}
