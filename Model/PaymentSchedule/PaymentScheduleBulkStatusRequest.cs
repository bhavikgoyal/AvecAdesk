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
    public List<StudentPaymentInstallmentUpdateRequest> PaymentList { get; set; } = [];

    public List<StudentCommissionDetailUpdateRequest> CommissionHistory { get; set; } = [];
}
public class StudentPaymentInstallmentUpdateRequest
{
    public int StudentPaymentInstallmentId { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string PaymentStatus { get; set; }
    public string? InstallmentImage { get; set; }
}
public class StudentCommissionDetailUpdateRequest
{
    public int CommissionDetailId { get; set; }
    public string CommissionStatus { get; set; }
}