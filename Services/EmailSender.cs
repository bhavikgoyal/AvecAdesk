using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.MailHistory;

namespace AvecADeskApi.Services;

public class EmailSender : IEmailSender
{
    // Their bodies carry one-time codes / reset links, so only the metadata goes to MailHistory.
    private static readonly HashSet<string> BodyNotStoredTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MailTypes.EmailVerification,
        MailTypes.PasswordReset,
    };

    // Always reach the real recipient, whatever MailMode:Mode is (the vendor needs its code to log in).
    private static readonly HashSet<string> AlwaysSendTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MailTypes.VendorOnboarding,
    };

    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;
    private readonly IMailHistoryRepository _mailHistory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmailSender(
        IConfiguration configuration,
        LogHelper logHelper,
        IMailHistoryRepository mailHistory,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _logHelper = logHelper;
        _mailHistory = mailHistory;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<MailSendResult> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default,
        string? mailType = null,
        string? fromEmail = null)
        => SendInternalAsync(toEmail, subject, htmlBody, null, null, null, mailType, fromEmail, cancellationToken);

    public Task<MailSendResult> SendWithAttachmentAsync(
        string toEmail,
        string subject,
        string htmlBody,
        byte[] attachmentBytes,
        string attachmentFileName,
        string contentType,
        CancellationToken cancellationToken = default,
        string? mailType = null)
        => SendInternalAsync(toEmail, subject, htmlBody, attachmentBytes, attachmentFileName, contentType, mailType, null, cancellationToken);

    private async Task<MailSendResult> SendInternalAsync(
        string toEmail,
        string subject,
        string htmlBody,
        byte[]? attachmentBytes,
        string? attachmentFileName,
        string? contentType,
        string? mailType,
        string? fromEmail,
        CancellationToken cancellationToken)
    {
        var from = string.IsNullOrWhiteSpace(fromEmail) ? _configuration["Email:FromAddress"] : fromEmail.Trim();
        var intendedToEmail = toEmail?.Trim();
        var mode = GetMailMode();
        var alwaysSend = mailType != null && AlwaysSendTypes.Contains(mailType.Trim());
        var testRecipient = (_configuration["MailMode:TestRecipient"] ?? string.Empty).Trim();
        var result = MailSendResult.Sent;
        string? errorMessage = null;
        string? deliveredTo = intendedToEmail;

        Attachment? attachment = null;
        MemoryStream? attachmentStream = null;
        try
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new InvalidOperationException("Recipient email is required.");

            // Demo / QA never reach the real recipient: only the test inbox, or nobody when none is set.
            if (mode != MailModes.Production && !alwaysSend)
            {
                if (string.IsNullOrWhiteSpace(testRecipient))
                {
                    result = MailSendResult.Skipped;
                    deliveredTo = null;
                    errorMessage = $"Not sent: MailMode is {mode} and no MailMode:TestRecipient is set.";
                    return result;
                }

                result = MailSendResult.SentToTestRecipient;
                subject = intendedToEmail!.Equals(testRecipient, StringComparison.OrdinalIgnoreCase)
                    ? $"[{mode}] {subject}"
                    : $"[{mode}] {subject} (for {intendedToEmail})";
                toEmail = testRecipient;
                deliveredTo = testRecipient;
            }

            var host = _configuration["Email:SmtpHost"];
            var port = _configuration.GetValue("Email:SmtpPort", 587);
            var fromName = _configuration["Email:FromName"] ?? "AVEC Global";
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var enableSsl = _configuration.GetValue("Email:EnableSsl", true);

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("Email SMTP is not configured. Set Email:SmtpHost and Email:FromAddress in appsettings.json.");

            using var message = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail.Trim());

            // The SMTP account may replace a From it is not allowed to send as; Reply-To still routes replies there.
            if (!string.IsNullOrWhiteSpace(fromEmail))
                message.ReplyToList.Add(new MailAddress(from));

            if (attachmentBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                attachmentStream = new MemoryStream(attachmentBytes);
                attachment = new Attachment(attachmentStream, attachmentFileName, contentType ?? "application/octet-stream");
                message.Attachments.Add(attachment);
            }

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
            };

            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new NetworkCredential(username, password);

            using var registration = cancellationToken.Register(client.SendAsyncCancel);
            await client.SendMailAsync(message, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logHelper.LogError(nameof(EmailSender), ex);
            throw;
        }
        finally
        {
            attachment?.Dispose();
            attachmentStream?.Dispose();

            var failed = errorMessage != null && result != MailSendResult.Skipped;
            await RecordHistoryAsync(new MailHistoryEntry
            {
                MailType = string.IsNullOrWhiteSpace(mailType) ? MailTypes.General : mailType.Trim(),
                ToEmail = deliveredTo,
                IntendedToEmail = intendedToEmail,
                MailMode = alwaysSend && mode != MailModes.Production ? $"{mode} (always sent)" : mode,
                FromEmail = from,
                Subject = subject,
                BodyHtml = mailType != null && BodyNotStoredTypes.Contains(mailType) ? null : htmlBody,
                AttachmentName = attachmentBytes is { Length: > 0 } ? attachmentFileName : null,
                Status = failed ? "Failed" : result == MailSendResult.Skipped ? "Skipped" : "Sent",
                ErrorMessage = errorMessage,
            });
        }
    }

    /// <summary>MailMode:Mode; anything missing or unknown counts as Demo so real recipients are never mailed by mistake.</summary>
    private string GetMailMode()
    {
        var mode = (_configuration["MailMode:Mode"] ?? string.Empty).Trim();
        if (mode.Equals(MailModes.Production, StringComparison.OrdinalIgnoreCase)) return MailModes.Production;
        if (mode.Equals(MailModes.QA, StringComparison.OrdinalIgnoreCase)) return MailModes.QA;
        return MailModes.Demo;
    }

    private async Task RecordHistoryAsync(MailHistoryEntry entry)
    {
        try
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                entry.SentByUserId = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
                entry.SentByUserName = user.FindFirstValue(ClaimTypes.Name);
            }
            else
            {
                entry.SentByUserName = "System";
            }

            await _mailHistory.AddAsync(entry);
        }
        catch (Exception ex)
        {
            // History is best effort: a failed insert must not turn a sent mail into an error.
            _logHelper.LogError($"{nameof(EmailSender)}.{nameof(RecordHistoryAsync)}", ex);
        }
    }
}
