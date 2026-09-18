namespace AvecADeskApi.Model.Student;

public class StudentUpdateRequest
{
    public int InstituteId { get; set; }
    public int? CourseId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public string? Assignment { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? EnrollmentFee { get; set; }
    public decimal? MaterialFee { get; set; }
    public decimal? TuitionFee { get; set; }     
    public decimal? OSHCFee { get; set; }
}
