using System.Globalization;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.MailHistory;
using AvecADeskApi.Model.Reminder;

namespace AvecADeskApi.Services;

/// <summary>
/// Sends the "Commission Report Request" mail (EmailTemplates category CommissionReportRequest) to the
/// "Account" contacts of every college whose commission due in the report month (by default the previous
/// month) is not yet Received, once per college + email per report month. Every college is logged in
/// CommissionReportRequestLog as Sent, Failed or NoContact.
/// </summary>
public class CommissionReportRequestService
{
    public const string SectionName = "CommissionReportRequest";
    private const string TemplateCategory = "CommissionReportRequest";
    private readonly ICommissionReportRequestRepository _repository;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;

    public CommissionReportRequestService(
        ICommissionReportRequestRepository repository,
        IEmailTemplateRepository templateRepository,
        IEmailSender emailSender,
        IConfiguration configuration,
        LogHelper logHelper)
    {
        _repository = repository;
        _templateRepository = templateRepository;
        _emailSender = emailSender;
        _configuration = configuration;
        _logHelper = logHelper;
    }

    private DateTime GetDefaultReportMonth(DateTime today)
    {
        var monthsBack = Math.Max(_configuration.GetValue($"{SectionName}:MonthsBack", 1), 0);
        return new DateTime(today.Year, today.Month, 1).AddMonths(-monthsBack);
    }

    /// <param name="force">Send even to emails that already got this month's mail (manual re-send).</param>
    public async Task<CommissionReportRequestRunResponse> RunAsync(
        DateTime? reportMonth,
        bool previewOnly,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        var section = _configuration.GetSection(SectionName);
        var month = reportMonth ?? GetDefaultReportMonth(DateTime.Today);
        month = new DateTime(month.Year, month.Month, 1);
        var monthLabel = month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        var recipients = await _repository.GetRecipientsAsync(month);

        var response = new CommissionReportRequestRunResponse
        {
            ReportMonth = monthLabel,
            Recipients = recipients,
            PreviewOnly = previewOnly,
        };

        if (recipients.Count == 0)
        {
            response.Message = $"No college has unreceived commission for {monthLabel}.";
            return response;
        }

        var noContact = recipients.Where(r => r.Email == null).ToList();
        var withEmail = recipients.Where(r => r.Email != null).ToList();
        var toSend = force ? withEmail : withEmail.Where(r => !r.AlreadySent).ToList();
        response.SkippedCount = withEmail.Count - toSend.Count;
        response.NoContactCount = noContact.Count;

        var template = await _templateRepository.GetEmailTemplateByCategoryAsync(TemplateCategory)
            ?? throw new InvalidOperationException(
                $"Email template with category '{TemplateCategory}' is missing. Run 02_Insert_EmailTemplate_CommissionReportRequest.sql.");

        var variables = new Dictionary<string, string>
        {
            ["ReportMonth"] = monthLabel,
            ["CompanyName"] = section["CompanyName"] ?? "Avec Global Group Pty Ltd",
        };
        var subject = ReplacePlaceholders(template.Subject, variables);
        response.Subject = subject;

        if (previewOnly)
        {
            response.Message = toSend.Count == 0
                ? $"No new {monthLabel} mail to send ({response.SkippedCount} already sent, {response.NoContactCount} college(s) without an Account contact)."
                : $"The {monthLabel} mail would be sent to {string.Join(", ", toSend.Select(r => $"{r.InstituteName} ({r.Email})"))}.";
            return response;
        }

        foreach (var college in noContact)
            await _repository.LogAsync(month, college, "NoContact", "No contact with designation 'Account' and an email.");

        var body = ReplacePlaceholders(template.BodyHtml, variables);

        // One mail per email address; every college it covers gets its own log row.
        foreach (var group in toSend.GroupBy(r => r.Email!, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var sendResult = await _emailSender.SendAsync(
                    group.Key,
                    subject,
                    body,
                    cancellationToken,
                    mailType: MailTypes.CommissionReportRequest,
                    fromEmail: section["FromAddress"]);

                // Only a real delivery counts as Sent; Demo/QA runs are logged but the college is mailed again in Production.
                var logStatus = sendResult switch
                {
                    MailSendResult.Sent => "Sent",
                    MailSendResult.SentToTestRecipient => "TestSent",
                    _ => "Skipped",
                };
                var logNote = sendResult == MailSendResult.Sent ? null : "Not mailed to the college: MailMode is not Production.";

                foreach (var college in group)
                {
                    if (sendResult == MailSendResult.Sent) response.SentCount++;
                    else response.NotMailedCount++;
                    await _repository.LogAsync(month, college, logStatus, logNote);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logHelper.LogError($"{nameof(CommissionReportRequestService)} ({group.Key})", ex);
                foreach (var college in group)
                {
                    response.FailedCount++;
                    await _repository.LogAsync(month, college, "Failed", ex.Message);
                }
            }
        }

        response.Message = $"Commission report request for {monthLabel}: {response.SentCount} sent, "
            + $"{response.NotMailedCount} not mailed (MailMode is not Production), {response.SkippedCount} already sent, "
            + $"{response.FailedCount} failed, {response.NoContactCount} without Account contact.";
        return response;
    }

    private static string ReplacePlaceholders(string? text, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        foreach (var kv in variables)
            text = text.Replace($"{{{{{kv.Key}}}}}", kv.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        return text;
    }
}
