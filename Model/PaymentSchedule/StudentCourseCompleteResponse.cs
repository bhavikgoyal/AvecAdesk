using System;

namespace AvecADeskApi.Model.PaymentSchedule;

public class StudentCourseCompleteResponse
{
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime StudentCreatedAt { get; set; }
    public DateTime? CourseStartDate { get; set; }
    public DateTime? CourseEndDate { get; set; }
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
    public decimal InstallmentAmount { get; set; }
    public DateTime? NextDueDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}
