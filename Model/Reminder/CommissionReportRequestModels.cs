namespace AvecADeskApi.Model.Reminder;

public class CommissionReportRequestRecipient
{
    public int ScrappingId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    /// <summary>Null when the college has no "Account" contact email.</summary>
    public string? Email { get; set; }
    public bool AlreadySent { get; set; }
}

public class CommissionReportRequestRunResponse
{
    public string ReportMonth { get; set; } = string.Empty;
    public List<CommissionReportRequestRecipient> Recipients { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public bool PreviewOnly { get; set; }
    public int SentCount { get; set; }
    /// <summary>Colleges that already got this month's mail.</summary>
    public int SkippedCount { get; set; }
    /// <summary>Demo / QA: not mailed to the college (skipped or sent to the test inbox).</summary>
    public int NotMailedCount { get; set; }
    public int FailedCount { get; set; }
    public int NoContactCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
