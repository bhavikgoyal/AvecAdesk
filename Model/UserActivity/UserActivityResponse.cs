namespace AvecADeskApi.Model.UserActivity
{
    public class UserActivityResponse
    {
        public int UserId { get; set; }
        public string? WorkDate { get; set; }
        public string? UserName { get; set; }
        public string? TotalTime { get; set; }
        public string? Productive { get; set; }
        public string? Neutral { get; set; }
    }
}
