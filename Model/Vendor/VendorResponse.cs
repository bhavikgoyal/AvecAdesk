namespace AvecADeskApi.Model.Vendor;

public class VendorResponse
{
    public int VendorId { get; set; }
    public int? UserId { get; set; }
    public string? VendorCode { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? BankDetails { get; set; }
    public string? CommissionPreference { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
