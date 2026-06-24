namespace AvecADeskApi.DTOs
{
    public class CreateChecklistRequest
    {
        public int CardID { get; set; }
        public string ChecklistTitle { get; set; } = string.Empty;
    }
}
