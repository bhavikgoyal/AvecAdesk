using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Agent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/agents")]
[ApiController]
[Authorize]
public class AgentController : ControllerBase
{
    private const string ManageRoles = "Admin,Super Admin";
    private readonly IAgentRepository _repository;
    private readonly LogHelper _logHelper;

    public AgentController(IAgentRepository repository, LogHelper logHelper)
    {
        _repository = repository;
        _logHelper = logHelper;
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> GetAgentLookup()
    {
        try
        {
            return Ok(await _repository.GetActiveNamesAsync());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetAgentLookup), ex);
            return StatusCode(500, "An error occurred while fetching agents.");
        }
    }

    [HttpGet]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> GetAgents()
    {
        try
        {
            return Ok(await _repository.GetAllAsync());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetAgents), ex);
            return StatusCode(500, "An error occurred while fetching agents.");
        }
    }

    [HttpGet("{agentId:int}")]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> GetAgent(int agentId)
    {
        try
        {
            var agent = await _repository.GetByIdAsync(agentId);
            return agent == null ? NotFound("Agent not found.") : Ok(agent);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetAgent), ex);
            return StatusCode(500, "An error occurred while fetching the agent.");
        }
    }

    [HttpPost]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> CreateAgent([FromBody] AgentUpsertRequest request)
    {
        try
        {
            var validationError = Validate(request);
            if (validationError != null)
                return BadRequest(validationError);

            var id = await _repository.CreateAsync(request, GetUserId());
            return Ok(await _repository.GetByIdAsync(id));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(CreateAgent), ex);
            return StatusCode(500, "An error occurred while creating the agent.");
        }
    }

    [HttpPost("{agentId:int}")]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> UpdateAgent(int agentId, [FromBody] AgentUpsertRequest request)
    {
        try
        {
            var validationError = Validate(request);
            if (validationError != null)
                return BadRequest(validationError);

            var updated = await _repository.UpdateAsync(agentId, request, GetUserId());
            if (!updated)
                return NotFound("Agent not found.");

            return Ok(await _repository.GetByIdAsync(agentId));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateAgent), ex);
            return StatusCode(500, "An error occurred while updating the agent.");
        }
    }

    /// <summary>Soft delete: the agent is hidden but stays in the table.</summary>
    [HttpPost("{agentId:int}/delete")]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> DeleteAgent(int agentId)
    {
        try
        {
            var deleted = await _repository.DeleteAsync(agentId, GetUserId());
            return deleted ? NoContent() : NotFound("Agent not found.");
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(DeleteAgent), ex);
            return StatusCode(500, "An error occurred while deleting the agent.");
        }
    }

    private int? GetUserId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    private static string? Validate(AgentUpsertRequest? request)
    {
        if (request == null)
            return "Request body is required.";
        if (string.IsNullOrWhiteSpace(request.AgentName))
            return "Agent name is required.";
        if (request.AgentName.Trim().Length > 200)
            return "Agent name must be 200 characters or fewer.";
        if (!string.IsNullOrWhiteSpace(request.PhoneNo) && !Regex.IsMatch(request.PhoneNo.Trim(), @"^\d{10}$"))
            return "Phone number must be exactly 10 digits.";
        if (!string.IsNullOrWhiteSpace(request.Email) && !MailAddress.TryCreate(request.Email.Trim(), out _))
            return "Email address is not valid.";
        return null;
    }
}
