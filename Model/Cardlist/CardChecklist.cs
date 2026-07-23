namespace AvecADeskApi.Model.Cardlist
{
    public class ChecklistModel
    {
        public int ChecklistID { get; set; }
        public int CardID { get; set; }
        public string ChecklistTitle { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public List<ChecklistItemModel> Items { get; set; } = new();
    }
    public class ChecklistItemModel
    {
        public int ChecklistItemID { get; set; }
        public int ChecklistID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int? Position { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class WeekChecklistItemModel
    {
        public int ChecklistItemID { get; set; }
        public int ChecklistID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int? Position { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string TrelloChecklistItemsId { get; set; } = string.Empty;
    }

}
