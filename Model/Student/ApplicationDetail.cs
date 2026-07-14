namespace AvecADeskApi.Model.Student
{
    public class ApplicationDetailResponse
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? PassportNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public bool AppliedVisaBefore { get; set; }
        public string? PreviousVisaType { get; set; }
        public bool RefusedVisa { get; set; }
        public string? RefusedCountry { get; set; }
        public string? RefusedVisaType { get; set; }
        public string? EnglishTestName { get; set; }
        public string? EnglishTestScore { get; set; }
        public DateTime? EnglishTestDate { get; set; }
        public string? HighestQualification { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ApplicationDetailRequest
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? PassportNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public bool AppliedVisaBefore { get; set; }
        public string? PreviousVisaType { get; set; }
        public bool RefusedVisa { get; set; }
        public string? RefusedCountry { get; set; }
        public string? RefusedVisaType { get; set; }
        public string? EnglishTestName { get; set; }
        public string? EnglishTestScore { get; set; }
        public DateTime? EnglishTestDate { get; set; }
        public string? HighestQualification { get; set; }
        public int? VendorId { get; set; }
    }
}
