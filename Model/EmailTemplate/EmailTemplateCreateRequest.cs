namespace AvecADeskApi.Model.EmailTemplate;

public class EmailTemplateCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
