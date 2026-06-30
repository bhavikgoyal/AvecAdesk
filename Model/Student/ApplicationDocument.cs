namespace AvecADeskApi.Model.Student
{
    public class ApplicationDocumentResponse
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string DocCategory { get; set; }
        public string DocType { get; set; }
        public string FileUrl { get; set; }
        public bool IsMandatory { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class ApplicationDocumentRequest
    {
        public string DocCategory { get; set; }  
        public string DocType { get; set; }
        public bool IsMandatory { get; set; }
        public IFormFile File { get; set; }
    }
}
