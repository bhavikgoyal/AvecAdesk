namespace AvecADeskApi.Model.Vendor;

public class VendorDocumentSaveRequest
{
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public int? FileSizeKB { get; set; }
}
