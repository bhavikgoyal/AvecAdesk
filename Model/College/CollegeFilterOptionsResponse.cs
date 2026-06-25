namespace AvecADeskApi.Model.College;

public class CollegeFilterOptionsResponse
{
    public List<string> Campuses { get; set; } = new();
    public List<string> States { get; set; } = new();
    public int PartnerCount { get; set; }
}
