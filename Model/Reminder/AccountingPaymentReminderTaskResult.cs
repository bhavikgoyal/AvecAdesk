namespace AvecADeskApi.Model.Reminder;

public class AccountingPaymentReminderTaskResult
{
    public int StudentPaymentInstallmentId { get; set; }
    public int? CardID { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public string CardTitle { get; set; } = string.Empty;
    public int? AssignedUserID { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
}

public class InvoiceFollowUpTaskResult
{
    public int InvoiceId { get; set; }
    public int? CardID { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CollegeName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>Checklist item added to the "Overdue invoice collection" card.</summary>
    public string ItemName { get; set; } = string.Empty;
    /// <summary>Names of the card members (every Admin / Accounting user).</summary>
    public string AssignedTo { get; set; } = string.Empty;
}

public class DuesTodoItemResponse
{
    public string ItemName { get; set; } = string.Empty;
    public DateTime? CardDate { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysLeft { get; set; }
    public int PendingUsers { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
}

public class DismissDuesTodoRequest
{
    public string ItemName { get; set; } = string.Empty;
    public DateTime? CardDate { get; set; }
}

public class AccountingPaymentReminderRunResponse
{
    public int CreatedCount { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<AccountingPaymentReminderTaskResult> Tasks { get; set; } = [];
}
