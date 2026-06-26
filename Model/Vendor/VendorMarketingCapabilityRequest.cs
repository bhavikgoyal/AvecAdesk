namespace AvecADeskApi.Model.Vendor;

public class VendorMarketingCapabilityRequest
{
    public int VendorId { get; set; }
    public int? BusinessId { get; set; }
    public string ConductsSeminars { get; set; } = string.Empty;
    public List<string>? MarketingChannels { get; set; }
    public string? MarketingChannelsOther { get; set; }
    public string InHouseVisaSupport { get; set; } = string.Empty;
}
