namespace AvecADeskApi.Model.Upload;

public class UploadDiffResponse
{
    public int UploadId { get; set; }
    public int InstituteId { get; set; }
    public string? ChangesSummary { get; set; }
    public int DiscrepancyCount { get; set; }
    public string ParseStatus { get; set; } = string.Empty;
}
