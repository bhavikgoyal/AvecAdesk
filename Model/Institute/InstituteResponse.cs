namespace AvecADeskApi.Model.Institute;

public class InstituteResponse
{
    public int InstituteId { get; set; }
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
    public bool IsPublished { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastScrapedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
