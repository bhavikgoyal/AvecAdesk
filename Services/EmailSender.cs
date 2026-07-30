using System.Net;
using System.Net.Mail;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;

namespace AvecADeskApi.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;

    public EmailSender(IConfiguration configuration, LogHelper logHelper)
    {
        _configuration = configuration;
        _logHelper = logHelper;
    }

    public Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
        => SendInternalAsync(toEmail, subject, htmlBody, null, null, null, cancellationToken);

    public Task SendWithAttachmentAsync(
        string toEmail,
        string subject,
        string htmlBody,
        byte[] attachmentBytes,
        string attachmentFileName,
        string contentType,
        CancellationToken cancellationToken = default)
        => SendInternalAsync(toEmail, subject, htmlBody, attachmentBytes, attachmentFileName, contentType, cancellationToken);

    private async Task SendInternalAsync(
        string toEmail,
        string subject,
        string htmlBody,
        byte[]? attachmentBytes,
        string? attachmentFileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new InvalidOperationException("Recipient email is required.");

        var host = _configuration["Email:SmtpHost"];
        var port = _configuration.GetValue("Email:SmtpPort", 587);
        var from = _configuration["Email:FromAddress"];
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

        Attachment? attachment = null;
        MemoryStream? attachmentStream = null;
        try
        {
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
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(EmailSender), ex);
            throw;
        }
        finally
        {
            attachment?.Dispose();
            attachmentStream?.Dispose();
        }
    }
}
