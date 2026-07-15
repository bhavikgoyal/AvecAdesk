namespace AvecADeskApi.Model.VendorStudent
{
  public class UpdateVendorStudentImmigrationRequest
  {
    public bool? VisaAppliedBefore { get; set; }
    public string? VisaAppliedType { get; set; }
    public bool? VisaRefused { get; set; }
    public string? RefusedVisaCountry { get; set; }
    public string? RefusedVisaType { get; set; }
  }
}
