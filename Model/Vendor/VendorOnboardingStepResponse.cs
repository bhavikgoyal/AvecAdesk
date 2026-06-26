namespace AvecADeskApi.Model.Vendor;

public class VendorOnboardingStepResponse
{
    public int VendorId { get; set; }
    public int? BusinessId { get; set; }
    public int? MarketId { get; set; }
    public int? PerformanceId { get; set; }
    public int? ComplianceId { get; set; }
    public int? BankingId { get; set; }
    public int? DeclarationId { get; set; }
    public string? VendorCode { get; set; }
    public string Message { get; set; } = string.Empty;
}
