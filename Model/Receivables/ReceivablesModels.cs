namespace AvecADeskApi.Model.Receivables;

public class AnticipatedReceivableResponse
{
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class OverdueReceivableResponse
{
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ReceivedPaymentResponse
{
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ReceivablesSummaryResponse
{
    public decimal TotalAnticipated { get; set; }
    public int AnticipatedCount { get; set; }
    public decimal TotalOverdue { get; set; }
    public int OverdueCount { get; set; }
    public decimal TotalReceived { get; set; }
    public int ReceivedCount { get; set; }
}

public class MonthRevenueDashboardResponse
{
    public decimal Revenue { get; set; }
    public decimal Collected { get; set; }
    public decimal Outstanding { get; set; }
    public decimal Forecast { get; set; }
}

public class StudentPaymentInstallmentResponse
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int ScheduleId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int StudentPaymentInstallmentId { get; set; }
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal FeesAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

// Common filter object used by the controller to pass query params down
public class ReceivablesFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? InstituteId { get; set; }
    public int? StudentId { get; set; }
}