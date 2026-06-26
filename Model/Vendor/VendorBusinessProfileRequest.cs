namespace AvecADeskApi.Model.Vendor;

public class VendorBusinessProfileRequest
{
    public int VendorId { get; set; }
    public int? BusinessId { get; set; }
    public string BusinessType { get; set; } = string.Empty;
    public string? BusinessTypeOther { get; set; }
    public int? NumberOfEmployees { get; set; }
    public int? NumberOfCounselors { get; set; }
    public int? NumberOfOffices { get; set; }
    public int? YearsOfExperience { get; set; }
}
