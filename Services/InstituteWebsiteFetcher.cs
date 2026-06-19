using System.Net;
using System.Text.RegularExpressions;
using AvecADeskApi.Interfaces;
using Microsoft.Playwright;

namespace AvecADeskApi.Services;

public class InstituteWebsiteFetcher : IInstituteWebsiteFetcher
{
    private const int MaxSectionPages = 10;
    private const int MaxProgramPages = 15;

    private readonly IHttpClientFactory _httpClientFactory;

    public InstituteWebsiteFetcher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(string? CombinedText, string? LogoUrl, List<string> Errors, bool UsedBrowser)> FetchWebsiteTextAsync(string websiteUrl)
    {
        var homepage = NormalizeHomepageUrl(websiteUrl);
        var errors = new List<string>();
        var textChunks = new List<string>();
        var fetchedHtml = new List<(string Url, string Html)>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedBrowser = false;
        string? logoUrl = null;
        BrowserSession? browserSession = null;

        try
        {
            async Task<(string? Html, string? Error)> FetchAsync(string url, bool preferBrowser)
            {
                if (preferBrowser)
                {
                    browserSession ??= await BrowserSession.CreateAsync();
                    return await browserSession.FetchAsync(url);
                }

                var httpResult = await TryFetchHtmlWithHttpAsync(url);
                if (!string.IsNullOrWhiteSpace(httpResult.Html))
                    return httpResult;

                usedBrowser = true;
                browserSession ??= await BrowserSession.CreateAsync();
                return await browserSession.FetchAsync(url);
            }

            async Task<bool> AddPageAsync(string url, bool preferBrowser)
            {
                var normalized = NormalizePageUrl(url);
                if (!visited.Add(normalized))
                    return false;

                var (html, error) = await FetchAsync(normalized, preferBrowser);
                if (string.IsNullOrWhiteSpace(html))
                {
                    if (!string.IsNullOrWhiteSpace(error))
                        errors.Add(error);
                    return false;
                }

                fetchedHtml.Add((normalized, html));
                var text = ExtractVisibleText(html);
                if (!string.IsNullOrWhiteSpace(text))
                    textChunks.Add($"--- Page: {normalized} ---\n{text}");

                return true;
            }

            await AddPageAsync(homepage, preferBrowser: false);

            var sectionUrls = DiscoverSectionUrls(homepage, fetchedHtml)
                .Take(MaxSectionPages)
                .ToList();

            foreach (var sectionUrl in sectionUrls)
                await AddPageAsync(sectionUrl, preferBrowser: usedBrowser);

            var programUrls = ExtractProgramLinks(fetchedHtml, homepage)
                .Where(url => !visited.Contains(NormalizePageUrl(url)))
                .Take(MaxProgramPages)
                .ToList();

            foreach (var programUrl in programUrls)
                await AddPageAsync(programUrl, preferBrowser: usedBrowser);

            if (textChunks.Count == 0)
                return (null, logoUrl, errors, usedBrowser);

            var combined = string.Join("\n\n", textChunks);
            if (combined.Length > 120000)
                combined = combined[..120000];

            logoUrl ??= ExtractLogoUrl(fetchedHtml, homepage);

            return (combined, logoUrl, errors, usedBrowser);
        }
        finally
        {
            if (browserSession is not null)
                await browserSession.DisposeAsync();
        }
    }

    private static string NormalizeHomepageUrl(string websiteUrl)
    {
        if (!Uri.TryCreate(websiteUrl.Trim(), UriKind.Absolute, out var uri))
            return websiteUrl.Trim();

        return $"{uri.Scheme}://{uri.Host}/";
    }

    private static string NormalizePageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.TrimEnd('/');

        var path = uri.AbsolutePath.TrimEnd('/');
        if (string.IsNullOrEmpty(path))
            path = "/";

        return $"{uri.Scheme}://{uri.Host}{path}";
    }

    private static IEnumerable<string> DiscoverSectionUrls(
        string homepage,
        IReadOnlyList<(string Url, string Html)> pages)
    {
        if (!Uri.TryCreate(homepage, UriKind.Absolute, out var baseUri))
            yield break;

        var origin = $"{baseUri.Scheme}://{baseUri.Host}";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in ExtractLinksFromHtml(pages, homepage, LooksLikeSectionLink))
        {
            if (seen.Add(url))
                yield return url;
        }

        foreach (var suffix in CommonStudyPaths)
        {
            var candidate = $"{origin}{suffix}";
            if (seen.Add(candidate))
                yield return candidate;
        }
    }

    private static readonly string[] CommonStudyPaths =
    {
        "/study-with-us",
        "/study-with-us/explore-our-programs",
        "/study-with-us/explore-our-programs/undergraduate",
        "/study-with-us/explore-our-programs/postgraduate",
        "/study",
        "/courses",
        "/programs",
        "/find-a-course",
        "/international-students",
    };

    private static IEnumerable<string> ExtractProgramLinks(
        IEnumerable<(string Url, string Html)> pages,
        string websiteUrl)
    {
        return ExtractLinksFromHtml(pages, websiteUrl, LooksLikeProgramLink);
    }

    private static IEnumerable<string> ExtractLinksFromHtml(
        IEnumerable<(string Url, string Html)> pages,
        string websiteUrl,
        Func<string, bool> linkFilter)
    {
        if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var baseUri))
            yield break;

        var origin = $"{baseUri.Scheme}://{baseUri.Host}";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var linkPattern = new Regex(
            @"href=[""'](?<href>/[^""'#?]+|https?://[^""'#?]+)[""']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (var (_, html) in pages)
        {
            foreach (Match match in linkPattern.Matches(html))
            {
                var href = WebUtility.HtmlDecode(match.Groups["href"].Value);
                if (!linkFilter(href))
                    continue;

                var absolute = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? href
                    : $"{origin}{href}";

                if (!Uri.TryCreate(absolute, UriKind.Absolute, out var linkUri))
                    continue;

                if (!string.Equals(linkUri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase))
                    continue;

                var normalized = NormalizePageUrl(absolute);
                if (seen.Add(normalized))
                    yield return normalized;
            }
        }
    }

    private static bool LooksLikeSectionLink(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return false;

        var value = href.ToLowerInvariant();
        if (value.Contains("login") || value.Contains("portal") || value.Contains("library"))
            return false;

        return value.Contains("study")
            || value.Contains("program")
            || value.Contains("course")
            || value.Contains("admission")
            || value.Contains("undergraduate")
            || value.Contains("postgraduate")
            || value.Contains("international")
            || value.Contains("find-a-course")
            || value.Contains("degree")
            || value.Contains("qualification")
            || value.Contains("offering")
            || value.Contains("training")
            || value.Contains("vet")
            || value.Contains("microcredential");
    }

    private static bool LooksLikeProgramLink(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return false;

        var value = href.ToLowerInvariant();
        if (value.Contains("login") || value.Contains("portal") || value.EndsWith(".pdf"))
            return false;

        return value.Contains("/program")
            || value.Contains("/course")
            || value.Contains("/study-with-us/")
            || value.Contains("/degrees/")
            || value.Contains("/degree/")
            || value.Contains("/bachelor")
            || value.Contains("/master")
            || value.Contains("/graduate")
            || value.Contains("/diploma")
            || value.Contains("/phd")
            || value.Contains("/qualification")
            || value.Contains("/vet/")
            || value.Contains("/certificate");
    }

    private async Task<(string? Html, string? Error)> TryFetchHtmlWithHttpAsync(string url)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("InstituteScraper");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Referer", url);

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return (null, $"{url} returned {(int)response.StatusCode} {response.ReasonPhrase}");

            var html = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(html) ? (null, $"{url} returned empty content.") : (html, null);
        }
        catch (Exception ex)
        {
            return (null, $"{url}: {ex.Message}");
        }
    }

    private static string ExtractVisibleText(string html)
    {
        var withoutScripts = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
        var withoutStyles = Regex.Replace(withoutScripts, @"<style[^>]*>[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);
        var withoutTags = Regex.Replace(withoutStyles, "<[^>]+>", " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        var normalized = Regex.Replace(decoded, @"\s+", " ").Trim();
        return normalized.Length > 25000 ? normalized[..25000] : normalized;
    }

    private static string? ExtractLogoUrl(IEnumerable<(string Url, string Html)> pages, string homepage)
    {
        if (!Uri.TryCreate(homepage, UriKind.Absolute, out var baseUri))
            return null;

        var origin = $"{baseUri.Scheme}://{baseUri.Host}";
        var scored = new List<(int Score, string Url)>();

        foreach (var (_, html) in pages)
        {
            if (string.IsNullOrWhiteSpace(html))
                continue;

            foreach (Match match in Regex.Matches(html, @"<img\b[^>]*>", RegexOptions.IgnoreCase))
            {
                var tag = match.Value;
                var src = ReadAttribute(tag, "src");
                if (string.IsNullOrWhiteSpace(src))
                    continue;

                var absolute = ToAbsoluteUrl(src, origin);
                if (string.IsNullOrWhiteSpace(absolute))
                    continue;

                var score = ScoreLogoCandidate(tag, absolute);
                if (score > 0)
                    scored.Add((score, absolute));
            }

            foreach (Match match in Regex.Matches(
                html,
                @"<meta[^>]+(?:property=[""']og:image[""']|name=[""']twitter:image[""'])[^>]+content=[""'](?<url>[^""']+)[""']|<meta[^>]+content=[""'](?<url>[^""']+)[""'][^>]+(?:property=[""']og:image[""']|name=[""']twitter:image[""'])",
                RegexOptions.IgnoreCase))
            {
                var absolute = ToAbsoluteUrl(match.Groups["url"].Value, origin);
                if (!string.IsNullOrWhiteSpace(absolute))
                    scored.Add((20, absolute));
            }

            foreach (Match match in Regex.Matches(
                html,
                @"<link[^>]+rel=[""'](?:apple-touch-icon|icon|shortcut icon)[""'][^>]+href=[""'](?<url>[^""']+)[""']|<link[^>]+href=[""'](?<url>[^""']+)[""'][^>]+rel=[""'](?:apple-touch-icon|icon|shortcut icon)[""']",
                RegexOptions.IgnoreCase))
            {
                var absolute = ToAbsoluteUrl(match.Groups["url"].Value, origin);
                if (!string.IsNullOrWhiteSpace(absolute))
                    scored.Add((absolute.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ? 5 : 10, absolute));
            }
        }

        return scored
            .OrderByDescending(item => item.Score)
            .Select(item => item.Url)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static int ScoreLogoCandidate(string imgTag, string absoluteUrl)
    {
        var tag = imgTag.ToLowerInvariant();
        var url = absoluteUrl.ToLowerInvariant();
        var score = 0;

        var alt = ReadAttribute(imgTag, "alt")?.ToLowerInvariant() ?? string.Empty;
        var cls = ReadAttribute(imgTag, "class")?.ToLowerInvariant() ?? string.Empty;
        var id = ReadAttribute(imgTag, "id")?.ToLowerInvariant() ?? string.Empty;

        if (alt is "logo" or "site logo" or "institute logo" or "university logo")
            score += 120;
        else if (alt.Contains("logo"))
            score += 90;

        if (cls.Contains("logo") || cls.Contains("top-nav") || cls.Contains("nav__img") || cls.Contains("site-logo") || cls.Contains("brand"))
            score += 80;

        if (id.Contains("logo") || id.Contains("brand"))
            score += 70;

        if (url.Contains("logo") || url.Contains("crest") || url.Contains("brand") || url.Contains("emblem"))
            score += 60;

        if (url.EndsWith(".svg") || url.EndsWith(".png") || url.EndsWith(".webp"))
            score += 15;

        if (url.EndsWith(".ico"))
            score -= 30;

        if (tag.Contains("header") || cls.Contains("header"))
            score += 10;

        // Skip tiny tracking pixels and unrelated images.
        if (url.Contains("pixel") || url.Contains("analytics") || url.Contains("banner") || url.Contains("hero"))
            score -= 40;

        return score;
    }

    private static string? ReadAttribute(string tag, string attributeName)
    {
        var match = Regex.Match(
            tag,
            $@"{attributeName}\s*=\s*[""'](?<value>[^""']+)[""']",
            RegexOptions.IgnoreCase);

        return match.Success ? WebUtility.HtmlDecode(match.Groups["value"].Value) : null;
    }

    private static string? ToAbsoluteUrl(string url, string origin)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var value = WebUtility.HtmlDecode(url.Trim());
        if (value.StartsWith("//", StringComparison.Ordinal))
            return $"https:{value}";

        if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return value;

        if (value.StartsWith('/'))
            return $"{origin}{value}";

        return $"{origin}/{value}";
    }

    private sealed class BrowserSession : IAsyncDisposable
    {
        private readonly IPlaywright _playwright;
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;

        private BrowserSession(IPlaywright playwright, IBrowser browser, IBrowserContext context)
        {
            _playwright = playwright;
            _browser = browser;
            _context = context;
        }

        public static async Task<BrowserSession> CreateAsync()
        {
            var playwright = await Playwright.CreateAsync();
            var browser = await LaunchBrowserAsync(playwright);
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
                Locale = "en-AU",
                ViewportSize = new ViewportSize { Width = 1366, Height = 900 },
            });

            return new BrowserSession(playwright, browser, context);
        }

        public async Task<(string? Html, string? Error)> FetchAsync(string url)
        {
            try
            {
                var page = await _context.NewPageAsync();
                try
                {
                    var response = await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 45000,
                    });

                    if (response is not null && !response.Ok)
                        return (null, $"{url} returned {(int)response.Status} via browser");

                    await page.WaitForTimeoutAsync(1500);

                    var html = await page.ContentAsync();
                    return string.IsNullOrWhiteSpace(html)
                        ? (null, $"{url} returned empty content via browser.")
                        : (html, null);
                }
                finally
                {
                    await page.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                return (null, $"{url} (browser): {SanitizeBrowserError(ex.Message)}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _context.CloseAsync();
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        private static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright)
        {
            foreach (var channel in new[] { "chrome", "msedge", (string?)null })
            {
                try
                {
                    var options = new BrowserTypeLaunchOptions { Headless = true };
                    if (!string.IsNullOrEmpty(channel))
                        options.Channel = channel;

                    return await playwright.Chromium.LaunchAsync(options);
                }
                catch (PlaywrightException) when (channel is not null)
                {
                    // Try next browser channel.
                }
            }

            throw new InvalidOperationException(
                "No browser available for scraping. Install Google Chrome or run: " +
                "powershell -ExecutionPolicy Bypass -File bin\\Debug\\net10.0\\playwright.ps1 install chromium");
        }

        private static string SanitizeBrowserError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Browser launch failed.";

            if (message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
            {
                return "Browser not installed. Run playwright.ps1 install chromium, then restart the API.";
            }

            var boxIndex = message.IndexOf('╔', StringComparison.Ordinal);
            if (boxIndex > 0)
                message = message[..boxIndex];

            return message.Trim();
        }
    }
}
