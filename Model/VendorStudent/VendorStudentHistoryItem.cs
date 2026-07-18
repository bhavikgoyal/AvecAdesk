namespace AvecADeskApi.Model.VendorStudent
{
  public class VendorStudentHistoryItem
  {
    public int StudentID { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public string? CountryToApply { get; set; }
    public string? CourseName { get; set; }
    public string? ApplicationStatus { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public int TotalRecords { get; set; }
  }
}
