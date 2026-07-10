namespace AvecADeskApi.Model.Student
{
    public class StudentApplicationResponse
    {
        public Guid Id { get; set; }
        public Guid? StudentId { get; set; }
        public int? CourseId { get; set; }
        public string? InstituteName { get; set; }
        public string? ProgramName { get; set; }
        public string CountryApplyingFor { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public ApplicationDetailResponse? Detail { get; set; }
        public List<ApplicationDocumentResponse>? Documents { get; set; }
        public DeclarationResponse? Declaration { get; set; }
    }

    public class StudentApplicationCreateRequest
    {
        public int? CourseId { get; set; }
        public string? InstituteName { get; set; }
        public string? ProgramName { get; set; }
        public string CountryApplyingFor { get; set; }
    }
    public class StudentApplicationDetailsModel
    {
        public Guid? Id { get; set; }
        public Guid? ApplicationId { get; set; }

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

        public bool? AppliedVisaBefore { get; set; }
        public string? PreviousVisaType { get; set; }

        public bool? RefusedVisa { get; set; }
        public string? RefusedCountry { get; set; }
        public string? RefusedVisaType { get; set; }

        public string? EnglishTestName { get; set; }
        public string? EnglishTestScore { get; set; }
        public DateTime? EnglishTestDate { get; set; }

        public string? HighestQualification { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TotalRecords { get; set; }
    }
}
