namespace AvecADeskApi.DTOs
{
    public class CreateChecklistItemRequest
    {
        public int ChecklistID { get; set; }
        public string ItemName { get; set; } = string.Empty;
    }
}
