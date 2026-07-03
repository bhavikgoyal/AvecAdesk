namespace AvecADeskApi.Model.AgrrementTemplate;

public class AgrrementTemplateCreateRequest
{
    public string TemplateName { get; set; } = string.Empty;
    public string AgreementType { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
}
