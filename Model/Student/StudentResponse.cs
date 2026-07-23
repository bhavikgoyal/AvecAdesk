namespace AvecADeskApi.Model.Student;

public class AllStudentResponse
{
    public int StudentId { get; set; }
    public int? ScrappingId { get; set; }
    public int? CourseId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public string EnrolmentStatus { get; set; } = string.Empty;
    public DateTime? AIHFormSubmittedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StudentResponse
{
    public int StudentId { get; set; }
    public int InstituteId { get; set; }
    public int? CourseId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public string EnrolmentStatus { get; set; } = string.Empty;
    public DateTime? AIHFormSubmittedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
