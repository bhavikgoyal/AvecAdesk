namespace AvecADeskApi.Model.Vendor;

public class VendorAgreementResponse
{
    public int AgreementId { get; set; }
    public int VendorId { get; set; }
    public string AgreementPath { get; set; } = string.Empty;
    public string AgreementType { get; set; } = string.Empty;
    public DateTime? SignedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public int? UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}
