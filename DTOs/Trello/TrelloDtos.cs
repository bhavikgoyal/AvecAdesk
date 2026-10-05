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
        public string IdList { get; set; } = string.Empty;
        public bool Closed { get; set; }
        public double Pos { get; set; }
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
        public List<string> Details { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }
}
