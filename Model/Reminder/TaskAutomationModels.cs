namespace AvecADeskApi.Model.Reminder;

// ---------- Contract expiry ----------

public class ContractExpiryTaskResult
{
    public string SourceType { get; set; } = string.Empty; // ContractExpiry_Institute | ContractExpiry_Student
    public int SourceReferenceId { get; set; }
    public string CardTitle { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int? CardID { get; set; }
    public int AssignedUserID { get; set; }
    public string Action { get; set; } = string.Empty; // WillCreate | AlreadyExists | Created
}

public class ContractExpiryTaskRunResponse
{
    public int CreatedCount { get; set; }
    public int DaysBefore { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ContractExpiryTaskResult> Tasks { get; set; } = [];
}

// ---------- Invoice due ----------

public class InvoiceDueTaskResult
{
    public int? CardID { get; set; }
    public int InvoiceId { get; set; }
    public string? CardTitle { get; set; }
    public int AssignedUserID { get; set; }
    public string Action { get; set; } = string.Empty; 
}

public class InvoiceDueTaskRunResponse
{
    public int CreatedCount { get; set; }
    public int MarkedDoneCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<InvoiceDueTaskResult> Tasks { get; set; } = [];
}
