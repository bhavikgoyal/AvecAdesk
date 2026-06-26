namespace AvecADeskApi.Model.EmployeeWorkHours
{
    public class StartStop
    {
        public int Id { get; set; }
        public int Userid { get; set; }
        public int? CardId { get; set; }
        public int? CardListItemId { get; set; }
        public string? CardTitle { get; set; }
        public string? ItemName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? StopTime { get; set; } = null;
        public bool? MarkDone { get; set; }
        public string? UserName { get; set; }
    }
}
