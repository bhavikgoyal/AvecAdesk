namespace AvecADeskApi.Model.Course;

public class CourseResponse
{
    public int CourseId { get; set; }
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public decimal? Fees { get; set; }
    public string? Duration { get; set; }
    public string? Eligibility { get; set; }
    public bool IsAIFetched { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public string? Campus { get; set; }
    public string? Level { get; set; }
    public string? ProgramLink { get; set; }
    public string? CricosCode { get; set; }
    public string? Intake { get; set; }
    public string? EnglishReq { get; set; }
    public string? ScholarshipsDetails { get; set; }
    public string? ProgramDescription { get; set; }
    public string? AddmissionRequirements { get; set; }
    public string? ProgramLogo { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RateType { get; set; } = string.Empty;
    public decimal? CommissionRate { get; set; }
}
public class CourseListResponse
{
    public int CourseId { get; set; }
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? ProgramLogo { get; set; }
    public string? ProgramLink { get; set; }
    public string? CourseCategory { get; set; }
    public string? Level { get; set; }
    public string? Campus { get; set; }
    public string? Intake { get; set; }
    public decimal? Fees { get; set; }
    public string? Duration { get; set; }
    public bool IsActive { get; set; }
}

public class InstituteScrappingCourseResponse
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