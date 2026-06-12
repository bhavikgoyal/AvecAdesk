using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.EmailTemplate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[Route("api/email-templates")]
[ApiController]
[Authorize]
public class EmailTemplatesController : ControllerBase
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly LogHelper _logHelper;

    public EmailTemplatesController(IEmailTemplateRepository emailTemplateRepository, LogHelper logHelper)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _logHelper = logHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmailTemplates()
    {
        try { return Ok(await _emailTemplateRepository.GetEmailTemplatesAsync()); }
        catch (Exception ex) { _logHelper.LogError(nameof(GetEmailTemplates), ex); return StatusCode(500, "An error occurred while fetching email templates."); }
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmailTemplate([FromBody] EmailTemplateCreateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Template name is required");

            var templateId = await _emailTemplateRepository.CreateEmailTemplateAsync(request);
            return Ok(await _emailTemplateRepository.GetEmailTemplateByIdAsync(templateId));
        }
        catch (Exception ex) { _logHelper.LogError(nameof(CreateEmailTemplate), ex); return StatusCode(500, "An error occurred while creating email template."); }
    }

    [HttpPut("{templateId:int}")]
    public async Task<IActionResult> UpdateEmailTemplate(int templateId, [FromBody] EmailTemplateUpdateRequest request)
    {
        try
        {
            if (!await _emailTemplateRepository.UpdateEmailTemplateAsync(templateId, request))
                return NotFound("Email template not found");

            return Ok(await _emailTemplateRepository.GetEmailTemplateByIdAsync(templateId));
        }
        catch (Exception ex) { _logHelper.LogError(nameof(UpdateEmailTemplate), ex); return StatusCode(500, "An error occurred while updating email template."); }
    }
}
