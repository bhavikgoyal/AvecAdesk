namespace AvecADeskApi.Model.Vendor;

public class VendorBankingRequest
{
    public int VendorId { get; set; }
    public string? BankName { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
    public string? SwiftCode { get; set; }
    public string? BankCountry { get; set; }
}
