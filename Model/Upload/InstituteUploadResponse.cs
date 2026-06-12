namespace AvecADeskApi.Model.Upload;

public class InstituteUploadResponse
{
    public int UploadId { get; set; }
    public int InstituteId { get; set; }
    public int UploadedByUserId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string ParseStatus { get; set; } = string.Empty;
    public string? ChangesSummary { get; set; }
    public int DiscrepancyCount { get; set; }
}
