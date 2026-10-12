namespace AvecADeskApi.Model.Institute;

public class InstituteUpdateRequest
{
    public int VendorId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColour { get; set; }
    public string? SecondaryColour { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ServiceTypes { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}
