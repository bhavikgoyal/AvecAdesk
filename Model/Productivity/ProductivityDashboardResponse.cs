namespace AvecADeskApi.Model.Productivity
{
    public class ProductivityDashboardResponse
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public List<ProductivityMemberResponse> Members { get; set; } = new();
        public List<ProductivityTrendPoint> Trend { get; set; } = new();
        public List<ProductivityDailyActivity> DailyActivity { get; set; } = new();
    }

    public class ProductivityDailyActivity
    {
        public string WorkDate { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public long TotalSeconds { get; set; }
        public long ProductiveSeconds { get; set; }
        public long NeutralSeconds { get; set; }
    }

    public class ProductivityMemberResponse
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? RoleName { get; set; }
        public long TotalSeconds { get; set; }
        public long ProductiveSeconds { get; set; }
        public long NeutralSeconds { get; set; }
        public int TasksTotal { get; set; }
        public int TasksDone { get; set; }
        public DateTime? FirstLoginOn { get; set; }
        public DateTime? LastSeenOn { get; set; }
        public int LoginDays { get; set; }
    }

    public class ProductivityTrendPoint
    {
        public string WorkDate { get; set; } = string.Empty;
        public long ProductiveSeconds { get; set; }
        public long NeutralSeconds { get; set; }
    }
}
