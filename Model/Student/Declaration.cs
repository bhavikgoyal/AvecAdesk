namespace AvecADeskApi.Model.Student
{
    public class DeclarationResponse
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public bool ApplicantSigned { get; set; }
        public DateTime? ApplicantSignatureDate { get; set; }
        public bool ParentSigned { get; set; }
        public DateTime? ParentSignatureDate { get; set; }
        public bool ChecklistAllSectionsCompleted { get; set; }
        public bool ChecklistAcademicTranscripts { get; set; }
        public bool ChecklistPassportCopy { get; set; }
        public bool ChecklistEnglishProficiency { get; set; }
        public bool ChecklistGSFormSubmitted { get; set; }
        public bool ChecklistDeclarationSigned { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DeclarationRequest
    {
        public bool ApplicantSigned { get; set; }
        public DateTime? ApplicantSignatureDate { get; set; }
        public bool ParentSigned { get; set; }
        public DateTime? ParentSignatureDate { get; set; }
        public bool ChecklistAllSectionsCompleted { get; set; }
        public bool ChecklistAcademicTranscripts { get; set; }
        public bool ChecklistPassportCopy { get; set; }
        public bool ChecklistEnglishProficiency { get; set; }
        public bool ChecklistGSFormSubmitted { get; set; }
        public bool ChecklistDeclarationSigned { get; set; }
    }
}
