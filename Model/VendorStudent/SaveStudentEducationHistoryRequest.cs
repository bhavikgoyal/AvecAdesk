namespace AvecADeskApi.Model.VendorStudent
{
  public class SaveStudentEducationHistoryRequest
  {
    public string? HighestQualification { get; set; }
    public bool? StudiedHighSchoolAustralia { get; set; }
    public bool? HasSecondaryPostSecondaryQual { get; set; }
    public string? HighSchoolDetails { get; set; }
    public string? QualificationDetails { get; set; }
  }
}
