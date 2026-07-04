namespace AvecADeskApi.Model.AgrrementTemplate;

public class AgrrementTemplateResponse
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string AgreementType { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
