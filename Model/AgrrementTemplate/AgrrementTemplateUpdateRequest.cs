namespace AvecADeskApi.Model.AgrrementTemplate;

public class AgrrementTemplateUpdateRequest
{
    public string TemplateName { get; set; } = string.Empty;
    public string AgreementType { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int CreatedByUserId { get; set; }
}
