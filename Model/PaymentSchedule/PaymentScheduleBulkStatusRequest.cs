namespace AvecADeskApi.Model.PaymentSchedule;

public class PaymentScheduleBulkStatusRequest
{
    public List<PaymentScheduleBulkStatusItem> Items { get; set; } = new();
}
public class UpdateStudentPaymentScheduleRequest
{
    public int StudentId { get; set; }
    public int NoOfInstallments { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public DateTime FirstDueDate { get; set; }
    public string? Phone { get; set; }
    public string? FolderNo { get; set; }
    public string? LeadNo { get; set; }

    // Bonus
    public decimal? Bonus { get; set; }
    public string? BonusType { get; set; }
    public string? BonusOption { get; set; }
    public DateTime? DueDate { get; set; }
    public List<StudentPaymentInstallmentUpdateRequest> PaymentList { get; set; } = [];

    public List<StudentCommissionDetailUpdateRequest> CommissionHistory { get; set; } = [];
}
public class StudentPaymentInstallmentUpdateRequest
{
    public int StudentPaymentInstallmentId { get; set; }
    public string? InstallmentNo { get; set; }
    public int? ParentInstallmentId { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal FeesAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidDate { get; set; }
    public string? InstallmentImage { get; set; }
    public string? FeeType { get; set; }
}
public class StudentCommissionDetailUpdateRequest
{
    public int CommissionDetailId { get; set; }

    public decimal CommissionAmount { get; set; }

    public decimal GSTAmount { get; set; }

    public decimal BonusAmount { get; set; }

    public decimal InvoiceAmount { get; set; }

    public string CommissionStatus { get; set; } = string.Empty;
}