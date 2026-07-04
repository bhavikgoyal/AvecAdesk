using AvecADeskApi.Model.EmailTemplate;

namespace AvecADeskApi.Interfaces;

public interface IEmailTemplateRepository
{
    Task<List<EmailTemplateResponse>> GetEmailTemplatesAsync();
    Task<EmailTemplateResponse?> GetEmailTemplateByIdAsync(int templateId);
    Task<int> CreateEmailTemplateAsync(EmailTemplateCreateRequest request);
    Task<bool> UpdateEmailTemplateAsync(int templateId, EmailTemplateUpdateRequest request);
    Task<bool> DeleteEmailTemplateAsync(int templateId);
}
