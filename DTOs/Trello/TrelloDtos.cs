namespace AvecADeskApi.DTOs.Trello
{
    public class TrelloMember
    {
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Username { get; set; }
    }

    public class TrelloBoard
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Url { get; set; }
        public bool Closed { get; set; }
    }

    public class TrelloList
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Closed { get; set; }
        public double Pos { get; set; }
    }

    public class TrelloCard
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Desc { get; set; }
        public DateTimeOffset? Due { get; set; }
        public DateTimeOffset? Start { get; set; }
        public string IdList { get; set; } = string.Empty;
        public bool Closed { get; set; }
        public double Pos { get; set; }
        public List<string> IdLabels { get; set; } = new();
        public TrelloCover? Cover { get; set; }
    }

    public class TrelloCover
    {
        public string? Color { get; set; }
        public string? IdAttachment { get; set; }
        public string? IdUploadedBackground { get; set; }
        public string? Size { get; set; }
        public string? Brightness { get; set; }
    }

    public class TrelloLabel
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Color { get; set; }
    }

    public class TrelloChecklist
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IdCard { get; set; } = string.Empty;
        public double Pos { get; set; }
        public List<TrelloCheckItem> CheckItems { get; set; } = new();
    }

    public class TrelloCheckItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? State { get; set; }
        public double Pos { get; set; }

        public bool IsComplete => string.Equals(State, "complete", StringComparison.OrdinalIgnoreCase);
    }

    public class TrelloCommentAction
    {
        public string Id { get; set; } = string.Empty;
        public string? IdMemberCreator { get; set; }
        public DateTimeOffset Date { get; set; }
        public TrelloCommentData? Data { get; set; }
        public TrelloMember? MemberCreator { get; set; }
    }

    public class TrelloCommentData
    {
        public string? Text { get; set; }
        public TrelloIdRef? Card { get; set; }
    }

    public class TrelloIdRef
    {
        public string Id { get; set; } = string.Empty;
    }

    public class TrelloActivityAction
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? IdMemberCreator { get; set; }
        public DateTimeOffset Date { get; set; }
        public TrelloActivityData? Data { get; set; }
        public TrelloMember? MemberCreator { get; set; }
        public TrelloMember? Member { get; set; }
    }

    public class TrelloActivityData
    {
        public TrelloActivityCard? Card { get; set; }
        public System.Text.Json.JsonElement? Old { get; set; }
        public TrelloNamedRef? CheckItem { get; set; }
        public TrelloNamedRef? Checklist { get; set; }
        public TrelloNamedRef? Attachment { get; set; }
        public TrelloNamedRef? Member { get; set; }
        public TrelloNamedRef? List { get; set; }
        public TrelloNamedRef? ListBefore { get; set; }
        public TrelloNamedRef? ListAfter { get; set; }
        public TrelloNamedRef? CardSource { get; set; }
    }

    public class TrelloActivityCard
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public DateTimeOffset? Due { get; set; }
        public bool? Closed { get; set; }
    }

    public class TrelloActivityImport
    {
        public int CardID { get; set; }
        public int UserID { get; set; }
        public string? TrelloMemberName { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string TrelloActivityId { get; set; } = string.Empty;
        /// <summary>Done by the token account, so it may be an AiDesk change pushed to Trello.</summary>
        public bool EchoOfAiDesk { get; set; }
        public string? EchoDescription { get; set; }
        public string? EchoNewValue { get; set; }
    }

    public class TrelloNamedRef
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? State { get; set; }
        public string? Url { get; set; }
    }

    public class TrelloWebhook
    {
        public string Id { get; set; } = string.Empty;
        public string IdModel { get; set; } = string.Empty;
        public string? CallbackURL { get; set; }
        public bool Active { get; set; }
    }

    public class TrelloStatusResponse
    {
        public bool Configured { get; set; }
        public bool Connected { get; set; }
        public string? FullName { get; set; }
        public string? Username { get; set; }
        public string? Error { get; set; }
        public bool AutoSyncEnabled { get; set; }
        public bool WebhookEnabled { get; set; }
        public string? WebhookCallbackUrl { get; set; }
        public int LinkedBoards { get; set; }
    }

    public class LocalBoardRow
    {
        public int BoardID { get; set; }
        public string BoardName { get; set; } = string.Empty;
        public string? TrelloBoardID { get; set; }
    }

    public class StoredWebhookRow
    {
        public string WebhookID { get; set; } = string.Empty;
        public string IdModel { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
    }

    public class TrelloBoardLinkResponse
    {
        public int LocalBoardID { get; set; }
        public string? LocalBoardName { get; set; }
        public string TrelloBoardID { get; set; } = string.Empty;
        public string? TrelloBoardName { get; set; }
        public string InitialWinner { get; set; } = "trello";
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public string? LastSyncStatus { get; set; }
    }

    public class TrelloMapRow
    {
        public string EntityType { get; set; } = string.Empty;
        public int LocalID { get; set; }
        public string TrelloID { get; set; } = string.Empty;
        public string? SyncHash { get; set; }
    }

    public static class TrelloEntityTypes
    {
        public const string Checklist = "checklist";
        public const string CheckItem = "checkitem";
        public const string Comment = "comment";
        public const string Label = "label";
        public const string CardLabels = "cardlabels";
        public const string CardCover = "cardcover";
        public const string CardStart = "cardstart";
    }

    public class LocalBoardLabelRow
    {
        public int BoardLabelID { get; set; }
        public string LabelName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class LocalCardDetailsRow
    {
        public int CardID { get; set; }
        public DateTime? StartDate { get; set; }
        public string? CoverColor { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? CoverSize { get; set; }
        public string? CoverBrightness { get; set; }
    }

    public class LocalChecklistRow
    {
        public int ChecklistID { get; set; }
        public int CardID { get; set; }
        public string ChecklistTitle { get; set; } = string.Empty;
        public string? TrelloChecklistID { get; set; }
        public List<LocalCheckItemRow> Items { get; set; } = new();
    }

    public class LocalCheckItemRow
    {
        public int ChecklistItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int? Position { get; set; }
        public string? TrelloItemID { get; set; }
        public string? SyncHash { get; set; }
    }

    public class LocalCommentRow
    {
        public int CommentID { get; set; }
        public int CardID { get; set; }
        public int UserID { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorTrelloId { get; set; }
        public string CommentText { get; set; } = string.Empty;
        public string? TrelloCommentID { get; set; }
        public string? SyncHash { get; set; }
    }

    public class TrelloLocalRef
    {
        public string? TrelloID { get; set; }
        public string? TrelloParentID { get; set; }
        public int CardID { get; set; }
        public int? BoardID { get; set; }
    }

    public class TrelloTombstoneRow
    {
        public string TrelloID { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? TrelloParentID { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class TrelloSyncResult
    {
        public bool DryRun { get; set; }
        public int ListsLinked { get; set; }
        public int ListsCreatedInAiDesk { get; set; }
        public int ListsCreatedInTrello { get; set; }
        public int CardsLinked { get; set; }
        public int CardsCreatedInAiDesk { get; set; }
        public int CardsCreatedInTrello { get; set; }
        public int CardsUpdatedInAiDesk { get; set; }
        public int CardsUpdatedInTrello { get; set; }
        public int CardsUnchanged { get; set; }
        public int CardsSkipped { get; set; }
        public int ChecklistChangesInAiDesk { get; set; }
        public int ChecklistChangesInTrello { get; set; }
        public int CommentChangesInAiDesk { get; set; }
        public int CommentChangesInTrello { get; set; }
        public int DeletesSentToTrello { get; set; }
        public int ActivitiesImported { get; set; }
        public int LabelChangesInAiDesk { get; set; }
        public int LabelChangesInTrello { get; set; }
        public int CardDetailChangesInAiDesk { get; set; }
        public int CardDetailChangesInTrello { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Details { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }
}
