namespace AvecADeskApi.Model.PaymentSchedule;

public class UploadInstallmentDocumentRequest
{
    public int StudentPaymentInstallmentId { get; set; }
    public string FileBase64 { get; set; } = string.Empty; 
    public string FileName { get; set; } = string.Empty;
}

public class UploadInstallmentDocumentResponse
{
    public bool Success { get; set; }
    public string DocumentUrl { get; set; } = string.Empty;
}

public class SendInstallmentConfirmationEmailRequest
{
    public int StudentPaymentInstallmentId { get; set; }
}

public class SendInstallmentConfirmationEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = "ConfirmedByStudent";
}

public class InstallmentConfirmationInfo
{
    public int StudentPaymentInstallmentId { get; set; }
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal FeesAmount { get; set; }
    public string? InstallmentImage { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? CourseName { get; set; }
    public DateTime? PaidDate { get; set; }
}