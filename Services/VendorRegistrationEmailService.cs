using System.Net;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Vendor;

namespace AvecADeskApi.Services;

public class VendorRegistrationEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;

    public VendorRegistrationEmailService(
        IEmailSender emailSender,
        IConfiguration configuration,
        LogHelper logHelper)
    {
        _emailSender = emailSender;
        _configuration = configuration;
        _logHelper = logHelper;
    }

    public async Task SendVendorInviteAsync(VendorResponse vendor, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vendor.Email))
            return;

        var frontendBaseUrl = (_configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var onboardingLink = $"{frontendBaseUrl}/onboarding?vendorId={vendor.VendorId}";
        var businessName = WebUtility.HtmlEncode(vendor.BusinessName);
        var contactName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(vendor.ContactPerson) ? "Partner" : vendor.ContactPerson);

        var subject = "Complete your vendor onboarding — AVEC Global";
        var body = $"""
            <p>Dear {contactName},</p>
            <p>Your vendor account for <strong>{businessName}</strong> has been created and is currently <strong>Pending</strong>.</p>
            <p>Please complete your onboarding using the link below:</p>
            <p><a href="{onboardingLink}" style="display:inline-block;padding:12px 20px;background:#2f80c9;color:#ffffff;text-decoration:none;border-radius:8px;font-weight:600;">Complete vendor onboarding</a></p>
            <p>Or copy this URL into your browser:<br/><a href="{onboardingLink}">{onboardingLink}</a></p>
            <p>Regards,<br/>AVEC Global Team</p>
            """;

        try
        {
            await _emailSender.SendAsync(vendor.Email, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(SendVendorInviteAsync), ex);
            throw;
        }
    }
}
