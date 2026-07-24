namespace AvecADeskApi.Model.PaymentSchedule;

public class PaymentScheduleCreateRequest
{
    public int StudentId { get; set; }
    public decimal TotalCourseFee { get; set; }
    public int NoOfInstallments { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public DateTime FirstDueDate { get; set; }
  
}
public class StudentPaymentInstallmentCreateRequest
{
    public int ScheduleId { get; set; }
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal FeesAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string PaymentStatus { get; set; } = "";
}
public class StudentCommissionCreateRequest
{
    public int ScheduleId { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal GSTPercentage { get; set; }
    public string? BonusType { get; set; }
    public string? BonusOption { get; set; }
}
public class StudentCommissionDetailCreateRequest
{
    public int CommissionId { get; set; }
    public int StudentPaymentInstallmentId { get; set; }

    public decimal CommissionAmount { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal InvoiceAmount { get; set; }

    public string? InvoiceNo { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? CommissionStatus { get; set; }
    public string? Remark { get; set; }
}