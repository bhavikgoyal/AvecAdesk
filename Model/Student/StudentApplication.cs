namespace AvecADeskApi.Model.Student
{
    public class StudentApplicationResponse
    {
        public Guid Id { get; set; }
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
        public string CountryApplyingFor { get; set; }
    }
}
