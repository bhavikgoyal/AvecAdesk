namespace AvecADeskApi.Model.PaymentSchedule;

public class PaymentScheduleResponse
{
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }
    public DateTime FirstDueDate { get; set; }
    public decimal? TotalCourseFee { get; set; }
    public int? NoOfInstallments { get; set; }
    public string? Frequency { get; set; }
    public DateTime CreatedDate { get; set; }
}
public class StudentPaymentScheduleListResponse
{
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;
    public string InstituteName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;

    public decimal TotalCourseFee { get; set; }
    public int NoOfInstallments { get; set; }
    public string Frequency { get; set; } = string.Empty;

    public DateTime FirstDueDate { get; set; }

    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public int PendingInstallments { get; set; }

    public decimal CollectedAmount { get; set; }
    public decimal BalanceAmount { get; set; }

    public DateTime? NextDueDate { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;
}
public class UpdateStudentPaymentScheduleResponse
{
    public int ScheduleId { get; set; }

    public int CommissionId { get; set; }
}
