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

public class AccountingPaymentReminderRunResponse
{
    public int CreatedCount { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<AccountingPaymentReminderTaskResult> Tasks { get; set; } = [];
}
