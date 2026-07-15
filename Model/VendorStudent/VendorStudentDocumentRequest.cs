namespace AvecADeskApi.Model.VendorStudent
{
  public class VendorStudentDocumentRequest
  {
    public string DocumentCategory { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
  }
}
