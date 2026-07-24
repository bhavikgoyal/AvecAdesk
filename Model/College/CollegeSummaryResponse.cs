namespace AvecADeskApi.Model.College;

public class CollegeSummaryResponse
{
    public string InstituteName { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? WebsiteURL { get; set; }
    public int ProgramCount { get; set; }
    public int CampusCount { get; set; }
    public bool TopColleged { get; set; }
    public List<string> Cities { get; set; } = new();
    public List<string> Campuses { get; set; } = new();
    public List<string> States { get; set; } = new();
}
