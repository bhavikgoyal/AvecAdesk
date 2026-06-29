namespace AvecADeskApi.Model.Vendor;

public class VendorOnboardingAdminSaveRequest
{
    public string? LegalBusinessName { get; set; }
    public string? TradingName { get; set; }
    public short? YearEstablished { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? CountryOfRegistration { get; set; }
    public string? RegisteredOfficeAddress { get; set; }
    public string? OperationalOfficeAddress { get; set; }
    public string? Website { get; set; }
    public string? LinkedInProfile { get; set; }

    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactDesignation { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactMobile { get; set; }
    public string? SecondaryContactName { get; set; }
    public string? SecondaryContactEmail { get; set; }
    public string? SecondaryContactNumber { get; set; }

    public string? BusinessType { get; set; }
    public string? BusinessTypeOther { get; set; }
    public int? NumberOfEmployees { get; set; }
    public int? NumberOfCounselors { get; set; }
    public int? NumberOfOffices { get; set; }
    public int? YearsOfExperience { get; set; }

    public string? PrimaryStudentSourceCountries { get; set; }
    public string? SecondaryMarkets { get; set; }
    public string? Top5Institutions { get; set; }
    public List<string>? DestinationCountries { get; set; }
    public string? DestinationCountriesOther { get; set; }

    public int? StudentsRecruitedLastYear { get; set; }
    public int? ExpectedStudentsNext12Months { get; set; }
    public decimal? VisaSuccessRate { get; set; }

    public string? RegisteredWithRegulatoryBody { get; set; }
    public string? RegulatoryBodyDetails { get; set; }
    public string? CertifiedCounselors { get; set; }
    public string? VisaFraudHistory { get; set; }
    public string? VisaFraudExplanation { get; set; }
    public List<string>? ComplianceAgreements { get; set; }

    public string? ConductsSeminars { get; set; }
    public List<string>? MarketingChannels { get; set; }
    public string? MarketingChannelsOther { get; set; }
    public string? InHouseVisaSupport { get; set; }

    public string? PreferredPaymentTerms { get; set; }

    public string? BankName { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
    public string? SwiftCode { get; set; }
    public string? BankCountry { get; set; }

    public string? AuthorizedSignatoryName { get; set; }
    public string? Signature { get; set; }
    public DateTime? DeclarationDate { get; set; }
    public List<string>? DeclarationItems { get; set; }

    public int? BusinessId { get; set; }
}
