namespace AvecADeskApi.Model.Course;

public class CourseUpdateRequest
{
    public int InstituteId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public decimal? Fees { get; set; }
    public string? Duration { get; set; }
    public string? Eligibility { get; set; }
}
