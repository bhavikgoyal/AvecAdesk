namespace AvecADeskApi.Interfaces;

public interface IInstituteWebsiteFetcher
{
    Task<(string? CombinedText, string? LogoUrl, List<string> Errors, bool UsedBrowser)> FetchWebsiteTextAsync(
        string websiteUrl,
        CancellationToken cancellationToken = default);
}
