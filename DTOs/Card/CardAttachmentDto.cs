namespace AvecADeskApi.DTOs.Card
{
    public class CardAttachmentResponse
    {
        public int AttachmentID { get; set; }
        public int CardID { get; set; }
        public bool IsLink { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long? FileSize { get; set; }
        public int? UploadedBy { get; set; }
        public string? UploadedByName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool IsCover { get; set; }
        public bool IsImage { get; set; }
    }

    public class AddCardAttachmentLinkRequest
    {
        public int CardID { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
    }

    public class UpdateCardAttachmentRequest
    {
        public string? DisplayName { get; set; }
        public string? LinkUrl { get; set; }
    }

    public class DeleteCardAttachmentResult
    {
        public bool Deleted { get; set; }
        public string? FileUrl { get; set; }
        public bool IsLink { get; set; }
    }

    public class NewCardAttachment
    {
        public int CardID { get; set; }
        public bool IsLink { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long? FileSize { get; set; }
    }
}
