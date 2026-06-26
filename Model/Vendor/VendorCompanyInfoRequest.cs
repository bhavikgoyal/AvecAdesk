namespace AvecADeskApi.Model.Vendor;

public class VendorCompanyInfoRequest
{
    public int? VendorId { get; set; }
    public string LegalBusinessName { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public short? YearEstablished { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? CountryOfRegistration { get; set; }
    public string? RegisteredOfficeAddress { get; set; }
    public string? OperationalOfficeAddress { get; set; }
    public string? Website { get; set; }
    public string? LinkedInProfile { get; set; }
}
