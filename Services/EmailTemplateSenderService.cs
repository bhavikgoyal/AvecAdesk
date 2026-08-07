using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;

namespace AvecADeskApi.Services;

public class EmailTemplateSenderService
{
    private readonly IEmailTemplateRepository _templateRepo;
    private readonly IEmailSender _emailSender;
    private readonly LogHelper _logHelper;

    public EmailTemplateSenderService(
        IEmailTemplateRepository templateRepo,
        IEmailSender emailSender,
        LogHelper logHelper)
    {
        _templateRepo = templateRepo;
        _emailSender = emailSender;
        _logHelper = logHelper;
    }

    /// <returns>true if a matching template was found and email sent</returns>
    public async Task<bool> TrySendByCategoryAsync(
        string category,
        string toEmail,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return false;

        try
        {
            var template = await _templateRepo.GetEmailTemplateByCategoryAsync(category);
            if (template == null)
                return false;

            var subject = ReplacePlaceholders(template.Subject, variables);
            var body = ReplacePlaceholders(template.BodyHtml, variables);

            await _emailSender.SendAsync(toEmail, subject, body, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(TrySendByCategoryAsync), ex);
            return false;
        }
    }

    private static string ReplacePlaceholders(string text, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        foreach (var kv in variables)
            text = text.Replace($"{{{{{kv.Key}}}}}", kv.Value ?? "", StringComparison.OrdinalIgnoreCase);
        return text;
    }
}