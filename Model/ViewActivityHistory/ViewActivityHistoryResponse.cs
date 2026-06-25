namespace AvecADeskApi.Model.ViewActivityHistory
{
    public class ViewActivityHistoryResponse
    {
        public int UserTrackingId { get; set; } = 0;
        public int UserId { get; set; } = 0;
        public string MachineName { get; set; } = string.Empty;
        public string StartOn { get; set; } = string.Empty;
        public string EndOn { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
        public List<SnapDetails> Snaps { get; set; } = new();
    }
    public class SnapDetails
    {
        public int UserSnapId { get; set; } = 0;
        public DateTime SnapOn { get; set; }
        public string? SnapThumbnailPath { get; set; }
        public string SnapOnTime => SnapOn.ToString("h:mm:ss tt");
        public string SnapPath { get; set; } = string.Empty;
        public int TotalClicks { get; set; } = 0;
        public string TaskName { get; set; } = string.Empty;
    }
}
