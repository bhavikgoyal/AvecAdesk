namespace AvecADeskApi.Model.VendorStudent
{
  public class VendorStudentDetailResponse
  {
    public int StudentID { get; set; }
    public int? VendorID { get; set; }
    public int? InstituteID { get; set; }
    public int? CourseID { get; set; }
    public string? CourseName { get; set; }
    public string? CountryToApply { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public string? Title { get; set; }
    public string? FamilyName { get; set; }
    public string? GivenNames { get; set; }
    public string? PreviousName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? CountryOfBirth { get; set; }
    public string? Citizenship { get; set; }
    public string? PassportNumber { get; set; }
    public DateTime? PassportExpiryDate { get; set; }
    public string? PassportCountryOfIssue { get; set; }
    public string? PassportFilePath { get; set; }
    public string? CurrentAddress { get; set; }
    public string? CurrentSuburb { get; set; }
    public string? CurrentState { get; set; }
    public string? CurrentCountry { get; set; }
    public string? CurrentPostcode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactEmail { get; set; }
    public string? AgentAgencyName { get; set; }
    public string? AgentContactPerson { get; set; }
    public string? AgentEmail { get; set; }
    public string? AgentTelephone { get; set; }
    public bool? VisaAppliedBefore { get; set; }
    public string? VisaAppliedType { get; set; }
    public bool? VisaRefused { get; set; }
    public string? RefusedVisaCountry { get; set; }
    public string? RefusedVisaType { get; set; }
    public string? EnglishTestType { get; set; }
    public string? EnglishOverallScore { get; set; }
    public DateTime? EnglishTestDate { get; set; }
    public string? EnglishEvidenceFilePath { get; set; }
    public bool? ChkCompletedAllSections { get; set; }
    public bool? ChkAgentCertifiedTranscripts { get; set; }
    public bool? ChkAgentCertifiedPassport { get; set; }
    public bool? ChkEnglishProficiencyEvidence { get; set; }
    public bool? ChkGSAssessmentFormSubmitted { get; set; }
    public bool? ChkReadSignedDeclaration { get; set; }
    public string? DeclarationName { get; set; }
    public string? ApplicantSignaturePath { get; set; }
    public DateTime? ApplicantSignatureDate { get; set; }
    public string? ParentGuardianName { get; set; }
    public string? ParentSignaturePath { get; set; }
    public DateTime? ParentSignatureDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public string? ApplicationStatus { get; set; }
    public List<StudentEducationItem> EducationHistory { get; set; } = [];
    public List<StudentDocumentItem> Documents { get; set; } = [];
  }

  public class StudentEducationItem
  {
    public int RecordID { get; set; }
    public int StudentID { get; set; }
    public string? HighestQualification { get; set; }
    public bool? StudiedHighSchoolAustralia { get; set; }
    public bool? HasSecondaryPostSecondaryQual { get; set; }
    public string? RecordType { get; set; }
    public string? InstitutionSchool { get; set; }
    public string? Course { get; set; }
    public string? LocationDetail { get; set; }
    public string? YearCommencedCompleted { get; set; }
  }

  public class StudentDocumentItem
  {
    public int DocumentID { get; set; }
    public int StudentID { get; set; }
    public string? DocumentCategory { get; set; }
    public string? DocumentType { get; set; }
    public string? FilePath { get; set; }
    public DateTime? UploadedDate { get; set; }
  }
}
