namespace AvecADeskApi.DTOs.Card
{
    public class CardCommentResponse
    {
        public int CommentID { get; set; }
        public int CardID { get; set; }
        public int UserID { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public bool IsEdited { get; set; }
        public bool CanEdit { get; set; }
    }

    public class CardActivityResponse
    {
        public int ActivityID { get; set; }
        public int? UserID { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? ActivityType { get; set; }
        public string? ActivityDescription { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class SaveCardCommentRequest
    {
        public int CardID { get; set; }
        public string CommentText { get; set; } = string.Empty;
    }

    public class LogCardActivityRequest
    {
        public int CardID { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
