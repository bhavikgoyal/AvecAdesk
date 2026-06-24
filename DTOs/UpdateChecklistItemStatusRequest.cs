namespace AvecADeskApi.DTOs
{
    public class UpdateChecklistItemStatusRequest
    {
        public int ChecklistItemID { get; set; }
        public bool IsCompleted { get; set; }
    }
}
