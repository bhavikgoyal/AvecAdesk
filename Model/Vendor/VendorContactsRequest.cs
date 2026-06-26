namespace AvecADeskApi.Model.Vendor;

public class VendorContactsRequest
{
    public int VendorId { get; set; }
    public string PrimaryContactName { get; set; } = string.Empty;
    public string? PrimaryContactDesignation { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactMobile { get; set; }
    public string? SecondaryContactName { get; set; }
    public string? SecondaryContactEmail { get; set; }
    public string? SecondaryContactNumber { get; set; }
}
