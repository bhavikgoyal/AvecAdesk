using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.AgrrementTemplate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/agrrement-templates")]
[ApiController]
[Authorize]
public class AgrrementTemplateController : ControllerBase
{
    private readonly IAgrrementTemplateRepository _agrrementTemplateRepository;
    private readonly LogHelper _logHelper;

    public AgrrementTemplateController(IAgrrementTemplateRepository agrrementTemplateRepository, LogHelper logHelper)
    {
        _agrrementTemplateRepository = agrrementTemplateRepository;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAgrrementTemplates()
    {
        try { return Ok(await _agrrementTemplateRepository.GetAgrrementTemplatesAsync()); }
        catch (Exception ex) { _logHelper.LogError(nameof(GetAgrrementTemplates), ex); return StatusCode(500, "An error occurred while fetching agrrement templates."); }
    }

    [HttpGet("{templateId:int}")]
    public async Task<IActionResult> GetAgrrementTemplateById(int templateId)
    {
        try
        {
            var template = await _agrrementTemplateRepository.GetAgrrementTemplateByIdAsync(templateId);
            return template is null ? NotFound("Agrrement template not found") : Ok(template);
        }
        catch (Exception ex) { _logHelper.LogError(nameof(GetAgrrementTemplateById), ex); return StatusCode(500, "An error occurred while fetching agrrement template."); }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAgrrementTemplate([FromBody] AgrrementTemplateCreateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TemplateName))
                return BadRequest("Template name is required");

            var templateId = await _agrrementTemplateRepository.CreateAgrrementTemplateAsync(request);
            return Ok(await _agrrementTemplateRepository.GetAgrrementTemplateByIdAsync(templateId));
        }
        catch (Exception ex) { _logHelper.LogError(nameof(CreateAgrrementTemplate), ex); return StatusCode(500, "An error occurred while creating agrrement template."); }
    }

    [HttpPut("{templateId:int}")]
    public async Task<IActionResult> UpdateAgrrementTemplate(int templateId, [FromBody] AgrrementTemplateUpdateRequest request)
    {
        try
        {
            if (!await _agrrementTemplateRepository.UpdateAgrrementTemplateAsync(templateId, request))
                return NotFound("Agrrement template not found");

            return Ok(await _agrrementTemplateRepository.GetAgrrementTemplateByIdAsync(templateId));
        }
        catch (Exception ex) { _logHelper.LogError(nameof(UpdateAgrrementTemplate), ex); return StatusCode(500, "An error occurred while updating agrrement template."); }
    }

    [HttpDelete("{templateId:int}")]
    public async Task<IActionResult> DeleteAgrrementTemplate(int templateId)
    {
        try
        {
            if (!await _agrrementTemplateRepository.DeleteAgrrementTemplateAsync(templateId))
                return NotFound("Agrrement template not found");

            return Ok(new { message = "Agrrement template deleted successfully." });
        }
        catch (Exception ex) { _logHelper.LogError(nameof(DeleteAgrrementTemplate), ex); return StatusCode(500, "An error occurred while deleting agrrement template."); }
    }
}
