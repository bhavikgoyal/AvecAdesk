namespace AvecADeskApi.Model.VendorStudent
{
  public class UpdateVendorStudentDeclarationRequest
  {
    public string? DeclarationName { get; set; }
    public string? ApplicantSignaturePath { get; set; }
    public DateTime? ApplicantSignatureDate { get; set; }
    public string? ParentGuardianName { get; set; }
    public string? ParentSignaturePath { get; set; }
    public DateTime? ParentSignatureDate { get; set; }
  }
}
