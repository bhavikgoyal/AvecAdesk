namespace AvecADeskApi.Model.Aih;

public class AihResponse
{
    public int InterestId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int InstituteId { get; set; }
    public int? CourseId { get; set; }
    public string? Notes { get; set; }
    public bool IsConvertedToLead { get; set; }
    public bool IsConvertedToStudent { get; set; }
    public int? StudentId { get; set; }
    public DateTime SubmittedAt { get; set; }
}
