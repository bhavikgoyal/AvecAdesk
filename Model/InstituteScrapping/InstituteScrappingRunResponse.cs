namespace AvecADeskApi.Model.InstituteScrapping;

public class InstituteScrappingRunResponse
{
    public int RecordsInserted { get; set; }
    public bool UsedAiFallback { get; set; }
    public string? Message { get; set; }
    public List<InstituteScrappingResponse> Records { get; set; } = new();
}
