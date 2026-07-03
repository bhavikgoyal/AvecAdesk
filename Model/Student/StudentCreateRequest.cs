namespace AvecADeskApi.Model.Student;

public class StudentCreateRequest
{
    public int InstituteId { get; set; }
    public int? CourseId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public string EnrolmentStatus { get; set; } = "Interested";
    
}
