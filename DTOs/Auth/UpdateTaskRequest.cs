namespace AvecADeskApi.DTOs.Auth
{
    public class UpdateTaskRequest
    {
        public int TaskId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
         public string? Status { get; set; }
    }
}
