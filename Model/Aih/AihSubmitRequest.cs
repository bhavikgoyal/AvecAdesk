namespace AvecADeskApi.Model.Aih;

public class AihSubmitRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int InstituteId { get; set; }
    public int? CourseId { get; set; }
    public string? Notes { get; set; }
}
