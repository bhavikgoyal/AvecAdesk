namespace AvecADeskApi.Model.Course;

public class CourseResponse
{
    public int CourseId { get; set; }
    public int InstituteId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public decimal? Fees { get; set; }
    public string? Duration { get; set; }
    public string? Eligibility { get; set; }
    public bool IsAIFetched { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
