namespace AvecADeskApi.Model.VendorStudent
{
  public class SaveVendorStudentRequest
  {
    public int? VendorID { get; set; }
    public int? InstituteID { get; set; }
    public int? CourseID { get; set; }

    // Basic Details
    public string? CountryToApply { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }

    // Personal Details
    public string? Title { get; set; }
    public string? FamilyName { get; set; }
    public string? GivenNames { get; set; }
    public string? PreviousName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    // Passport Details
    public string? CountryOfBirth { get; set; }
    public string? Citizenship { get; set; }
    public string? PassportNumber { get; set; }
    public DateTime? PassportExpiryDate { get; set; }
    public string? PassportCountryOfIssue { get; set; }
    public string? PassportFilePath { get; set; }

    // Current Residential Address
    public string? CurrentAddress { get; set; }
    public string? CurrentSuburb { get; set; }
    public string? CurrentState { get; set; }
    public string? CurrentCountry { get; set; }
    public string? CurrentPostcode { get; set; }

    // Emergency Contact
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactEmail { get; set; }
  }
}
