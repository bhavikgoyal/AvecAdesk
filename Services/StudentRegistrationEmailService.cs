
using AvecADeskApi.Model.EmailTemplate;

namespace AvecADeskApi.Services;

public class StudentRegistrationEmailService
{
    private readonly EmailTemplateSenderService _templateSender;

    public StudentRegistrationEmailService(EmailTemplateSenderService templateSender)
    {
        _templateSender = templateSender;
    }

    public async Task SendWelcomeEmailAsync(StudentEmailInfo student, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(student.Email))
            return;

        var variables = new Dictionary<string, string>
        {
            ["StudentName"] = student.FullName ?? "",
            ["enrollmentNumber"] = student.EnrollmentNumber ?? "",
            ["PortalLink"] = "http://localhost:5173/student-login", 
        };

        await _templateSender.TrySendByCategoryAsync("Onboarding", student.Email, variables, cancellationToken);
    }
}