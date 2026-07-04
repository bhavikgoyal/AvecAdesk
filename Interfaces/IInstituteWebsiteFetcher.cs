namespace AvecADeskApi.Interfaces;

using AvecADeskApi.Model.InstituteScrapping;

public interface IInstituteWebsiteFetcher
{
    Task<(string? CombinedText, string? LogoUrl, List<string> Errors, bool UsedBrowser, List<ScrapedProgramRecord> ParsedPrograms)> FetchWebsiteTextAsync(string websiteUrl);
}
