using AvecADeskApi.Model.MailHistory;

namespace AvecADeskApi.Interfaces;

public interface IEmailSender
{
    /// <param name="mailType">Recorded in MailHistory (see Model.MailHistory.MailTypes).</param>
    /// <param name="fromEmail">Overrides Email:FromAddress for this mail only.</param>
    /// <returns>Whether the real recipient got it; in Demo/QA mode it is redirected or skipped (MailMode:Mode).</returns>
    Task<MailSendResult> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default,
        string? mailType = null,
        string? fromEmail = null);

    Task<MailSendResult> SendWithAttachmentAsync(
        string toEmail,
        string subject,
        string htmlBody,
        byte[] attachmentBytes,
        string attachmentFileName,
        string contentType,
        CancellationToken cancellationToken = default,
        string? mailType = null);
}
