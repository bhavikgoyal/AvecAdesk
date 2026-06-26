namespace AvecADeskApi.Model.Vendor;

public class VendorMarketsRequest
{
    public int VendorId { get; set; }
    public string? PrimaryStudentSourceCountries { get; set; }
    public string? SecondaryMarkets { get; set; }
    public string? Top5Institutions { get; set; }
    public List<string>? DestinationCountries { get; set; }
    public string? DestinationCountriesOther { get; set; }
}
