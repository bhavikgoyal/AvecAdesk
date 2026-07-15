namespace AvecADeskApi.Model.VendorStudent
{
  public class UpdateVendorStudentChecklistRequest
  {
    public bool ChkCompletedAllSections { get; set; }
    public bool ChkAgentCertifiedTranscripts { get; set; }
    public bool ChkAgentCertifiedPassport { get; set; }
    public bool ChkEnglishProficiencyEvidence { get; set; }
    public bool ChkGSAssessmentFormSubmitted { get; set; }
    public bool ChkReadSignedDeclaration { get; set; }
  }
}
