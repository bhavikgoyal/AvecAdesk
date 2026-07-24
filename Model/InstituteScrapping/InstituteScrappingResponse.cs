namespace AvecADeskApi.Model.InstituteScrapping;

public class InstituteScrappingResponse
{
    public int ScrappingId { get; set; }
    public string? InstituteName { get; set; }
    public string? WebsiteURL { get; set; }
    public string? Campus { get; set; }
    public string? State { get; set; }
    public string? ProgramName { get; set; }
    public string? Level { get; set; }
    public string? ProgramLink { get; set; }
    public string? CricosCode { get; set; }
    public string? Duration { get; set; }
    public string? Intake { get; set; }
    public string? FeesYearly { get; set; }
    public string? EnglishReq { get; set; }
    public string? Name { get; set; }
    public string? Logo { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Description { get; set; }
    public string? CountryRanking { get; set; }
    public string? ScholarshipsDetails { get; set; }
    public string? ProgramDescription { get; set; }
    public string? ProgramLogo { get; set; }
    public string? AddmissionRequirements { get; set; }
    public bool? IsScrap { get; set; }
    public DateTime? CreatedAt { get; set; }
}
public class InstituteNameResponse
{
    public int ScrappingId { get; set; }

    public string? InstituteName { get; set; }
    public string? Campus { get; set; }
}