namespace AvecADeskApi.DTOs.Auth
{
    public class CreateTaskRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string TaskType { get; set; } 
        public int AssignedUserId { get; set; }
        public int Priority { get; set; }
    }
}
