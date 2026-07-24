namespace AvecADeskApi.Model.Student;

public class AllStudentResponse
{
    public int StudentId { get; set; }
    public int? ScrappingId { get; set; }
    public int? CourseId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public string EnrolmentStatus { get; set; } = string.Empty;
    public DateTime? AIHFormSubmittedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StudentResponse
{
    public int StudentId { get; set; }
    public int InstituteId { get; set; }
    public int? CourseId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public string EnrolmentStatus { get; set; } = string.Empty;
    public DateTime? AIHFormSubmittedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StudentPaymentScheduleDetailResponse
{
    // Student
    public int StudentId { get; set; }

    public int InstituteId { get; set; }
    public string? InstituteName { get; set; }

    public int? CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? Campus { get; set; }

    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FolderNo { get; set; }

    public DateTime? CourseStartDate { get; set; }
    public DateTime? CourseEndDate { get; set; }

    // Payment Schedule
    public int ScheduleId { get; set; }
    public DateTime FirstDueDate { get; set; }
    public decimal TotalCourseFee { get; set; }
    public int NoOfInstallments { get; set; }
    public string? Frequency { get; set; }

    // Commission
    public int? CommissionId { get; set; }
    public decimal? CommissionPercentage { get; set; }
    public decimal? GSTPercentage { get; set; }
    public string? BonusType { get; set; }
    public string? BonusOption { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? CommissionAmount { get; set; }
    public decimal? GSTAmount { get; set; }
    public decimal? BonusAmount { get; set; }
    public string? Remark { get; set; }
    // Student Payment List
    public List<StudentPaymentItem> StudentPaymentList { get; set; } = [];
    // Commission History
    public List<CommissionHistoryItem> CommissionHistory { get; set; } = [];
}

public class StudentPaymentItem
{
    public int StudentPaymentInstallmentId { get; set; }
    public int ScheduleId { get; set; }
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal FeesAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? PaidDate { get; set; }
}

public class CommissionHistoryItem
{
    public int CommissionDetailId { get; set; }

    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal FeesAmount { get; set; }
    public string? PaymentStatus { get; set; }

    public decimal CommissionAmount { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal InvoiceAmount { get; set; }

    public string? InvoiceNo { get; set; }
    public DateTime? ReceivedDate { get; set; }

    public string? CommissionStatus { get; set; }
    public string? Remark { get; set; }
}