namespace AvecADeskApi.Model.Vendor;

public class VendorAgreementUploadRequest
{
    public string AgreementType { get; set; } = "Initial";
    public DateTime? SignedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
