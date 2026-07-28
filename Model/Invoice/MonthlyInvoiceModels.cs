namespace AvecADeskApi.Model.Invoice;

public class MonthlyPaidInstallmentRow
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string FolderNo { get; set; } = string.Empty;
    public int? CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Campus { get; set; } = string.Empty;
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public int ScheduleId { get; set; }
    public int StudentPaymentInstallmentId { get; set; }
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal FeesAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime? PaidDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public int? CommissionDetailId { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal InvoiceAmount { get; set; }
    public decimal GSTPercentage { get; set; }
}

public class MonthlyInvoiceGenerateRequest
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? InstituteId { get; set; }
    public string? Campus { get; set; }
}

public class MonthlyInvoiceGenerateResult
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<MonthlyInvoiceResultItem> Invoices { get; set; } = [];
}

public class MonthlyInvoiceResultItem
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int StudentCount { get; set; }
    public string? DocumentPath { get; set; }
    public bool EmailSent { get; set; }
}
