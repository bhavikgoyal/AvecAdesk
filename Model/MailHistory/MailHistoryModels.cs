namespace AvecADeskApi.Model.MailHistory;

public static class MailTypes
{
    public const string General = "General";
    public const string EmailVerification = "EmailVerification";
    public const string PasswordReset = "PasswordReset";
    public const string VendorOnboarding = "Vendor Onboarding";
    public const string MonthlyInvoice = "MonthlyInvoice";
    public const string InstallmentConfirmation = "InstallmentConfirmation";
    public const string CommissionReportRequest = "CommissionReportRequest";
}

public enum MailSendResult
{
    Sent,
    SentToTestRecipient,
    Skipped,
}

public static class MailModes
{
    public const string Demo = "Demo";
    public const string QA = "QA";
    public const string Production = "Production";
}

public class MailHistoryEntry
{
    public string MailType { get; set; } = MailTypes.General;
    public string? ToEmail { get; set; }
    public string? IntendedToEmail { get; set; }
    public string? MailMode { get; set; }
    public string? FromEmail { get; set; }
    public string? Subject { get; set; }
    public string? BodyHtml { get; set; }
    public string? AttachmentName { get; set; }
    public string Status { get; set; } = "Sent";
    public string? ErrorMessage { get; set; }
    public int? SentByUserId { get; set; }
    public string? SentByUserName { get; set; }
}

public class MailHistoryQuery
{
    public string? Search { get; set; }
    public string? MailType { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class MailHistoryListItem
{
    public int MailHistoryId { get; set; }
    public string MailType { get; set; } = string.Empty;
    public string? ToEmail { get; set; }
    public string? IntendedToEmail { get; set; }
    public string? MailMode { get; set; }
    public string? Subject { get; set; }
    public string? AttachmentName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? SentByUserName { get; set; }
    public DateTime SentOn { get; set; }
}

public class MailHistoryDetail : MailHistoryListItem
{
    public string? FromEmail { get; set; }
    public string? BodyHtml { get; set; }
    public int? SentByUserId { get; set; }
}

public class MailHistoryPagedResult
{
    public List<MailHistoryListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
}
