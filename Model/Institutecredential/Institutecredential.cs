namespace AvecADeskApi.Model.InstituteCredential;

public class InstituteCredentialDto
{
    public int Id { get; set; }
    public int InstituteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class InstituteCredentialUpsertRequest
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}