namespace AvecADeskApi.DTOs
{
    public class UpdateItemAssigneeRequest
    {
        public int ChecklistItemID { get; set; }
        public int AssignedUserID { get; set; }
    }
}
