namespace AvecADeskApi.Model.Vendor;

public class VendorComplianceRequest
{
    public int VendorId { get; set; }
    public string RegisteredWithRegulatoryBody { get; set; } = string.Empty;
    public string? RegulatoryBodyDetails { get; set; }
    public string CertifiedCounselors { get; set; } = string.Empty;
    public string VisaFraudHistory { get; set; } = string.Empty;
    public string? VisaFraudExplanation { get; set; }
    public List<string>? ComplianceAgreements { get; set; }
}
