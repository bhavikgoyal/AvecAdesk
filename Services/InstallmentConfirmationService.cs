using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;

namespace AvecADeskApi.Services;

public class InstallmentConfirmationService
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IEmailSender _emailSender;
    private readonly IWebHostEnvironment _env;
    private readonly LogHelper _logHelper;

    public InstallmentConfirmationService(
        IScheduleRepository scheduleRepository,
        IEmailSender emailSender,
        IWebHostEnvironment env,
        LogHelper logHelper)
    {
        _scheduleRepository = scheduleRepository;
        _emailSender = emailSender;
        _env = env;
        _logHelper = logHelper;
    }

    public async Task<(bool Success, string Message)> SendConfirmationEmailAsync(int studentPaymentInstallmentId, CancellationToken cancellationToken = default)
    {
        var info = await _scheduleRepository.GetInstallmentConfirmationInfoAsync(studentPaymentInstallmentId);
        if (info == null)
            return (false, "Installment not found.");

        if (string.IsNullOrWhiteSpace(info.InstallmentImage))
            return (false, "Please upload a document before sending email.");

        if (string.IsNullOrWhiteSpace(info.Email))
            return (false, "Student does not have an email address on file.");

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var relativePath = info.InstallmentImage.TrimStart('/');
        var fullPath = Path.Combine(webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
            return (false, "Uploaded document could not be found on the server.");

        var fileBytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        var fileName = Path.GetFileName(fullPath);
        var contentType = Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream",
        };

        var subject = $"Payment Confirmation — Installment #{info.InstallmentNo}";
        var html = $"""
            <p>Dear {info.FullName},</p>
            <p>Please find attached the confirmation document for your installment payment.</p>
            <p><b>Course:</b> {info.CourseName}<br/>
            <b>Installment #:</b> {info.InstallmentNo}<br/>
            <b>Due Date:</b> {info.DueDate:dd-MM-yyyy}<br/>
            <b>Amount:</b> {info.FeesAmount:0.00}</p>
            <p>Regards,<br/>AVEC Global</p>
            """;

        try
        {
            await _emailSender.SendWithAttachmentAsync(
                info.Email, subject, html, fileBytes, fileName, contentType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstallmentConfirmationService)}.{nameof(SendConfirmationEmailAsync)}", ex);
            return (false, "Failed to send email. Please try again.");
        }

        var confirmed = await _scheduleRepository.ConfirmInstallmentByStudentAsync(studentPaymentInstallmentId);
        if (!confirmed)
            return (false, "Email sent, but failed to update installment status.");

        return (true, "Confirmation email sent successfully.");
    }
}