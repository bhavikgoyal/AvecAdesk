namespace AvecADeskApi.Model.Vendor;

public class VendorCommercialTermsRequest
{
    public int VendorId { get; set; }
    public int? BusinessId { get; set; }
    public string PreferredPaymentTerms { get; set; } = string.Empty;
}
