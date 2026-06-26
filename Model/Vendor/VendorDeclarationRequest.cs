namespace AvecADeskApi.Model.Vendor;

public class VendorDeclarationRequest
{
    public int VendorId { get; set; }
    public List<string>? DeclarationItems { get; set; }
    public string AuthorizedSignatoryName { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public DateTime? DeclarationDate { get; set; }
}
