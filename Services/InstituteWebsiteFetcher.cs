using System.Net;
using System.Text.RegularExpressions;
using AvecADeskApi.Interfaces;
using Microsoft.Playwright;

namespace AvecADeskApi.Services;

public class InstituteWebsiteFetcher : IInstituteWebsiteFetcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const int DefaultProgramPageCap = 250;
    private const int MaxConsecutiveFetchFailures = 10;
    private const int MaxParallelProgramFetches = 4;

    private readonly int _maxListingPages;
    private readonly int _maxSectionPages;
    private readonly int _maxProgramPages;
    private readonly int _maxCombinedTextLength;
    private readonly int _maxPageTextLength;
    private readonly int _maxTotalSeconds;
    private readonly int _browserPageTimeoutMs;

    public InstituteWebsiteFetcher(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _maxListingPages = configuration.GetValue("Scraping:MaxListingPages", 20);
        _maxSectionPages = configuration.GetValue("Scraping:MaxSectionPages", 0);
        var configuredProgramPages = configuration.GetValue("Scraping:MaxProgramPages", 0);
        _maxProgramPages = configuredProgramPages > 0 ? configuredProgramPages : DefaultProgramPageCap;
        _maxCombinedTextLength = configuration.GetValue("Scraping:MaxCombinedTextLength", 400_000);
        _maxPageTextLength = configuration.GetValue("Scraping:MaxPageTextLength", 6000);
        _maxTotalSeconds = configuration.GetValue("Scraping:MaxTotalSeconds", 600);
        _browserPageTimeoutMs = configuration.GetValue("Scraping:BrowserPageTimeoutMs", 12_000);
    }

    public async Task<(string? CombinedText, string? LogoUrl, List<string> Errors, bool UsedBrowser)> FetchWebsiteTextAsync(
        string websiteUrl,
        CancellationToken cancellationToken = default)
    {
        var homepage = NormalizeHomepageUrl(websiteUrl);
        var errors = new List<string>();
        var textChunks = new List<string>();
        var fetchedHtml = new List<(string Url, string Html)>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedBrowser = false;
        var consecutiveFailures = 0;
        var skipBrowserForSession = false;
        string? logoUrl = null;
        BrowserSession? browserSession = null;
        var startedAt = DateTime.UtcNow;

        bool IsTimeBudgetExceeded() =>
            _maxTotalSeconds > 0 && (DateTime.UtcNow - startedAt).TotalSeconds >= _maxTotalSeconds;

        void EnsureNotTimedOut()
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException("Website scraping was cancelled.");
        }

        try
        {
            async Task<(string? Html, string? Error)> FetchAsync(string url, bool preferBrowser)
            {
                var httpResult = await TryFetchHtmlWithHttpAsync(url);

                if (!string.IsNullOrWhiteSpace(httpResult.Html) && HasUsableHttpContent(httpResult.Html))
                    return (httpResult.Html, null);

                var blockedByHttp = IsSkippableStatus(httpResult.StatusCode);
                if (blockedByHttp)
                    skipBrowserForSession = true;

                var shouldTryBrowser = !skipBrowserForSession
                    && (blockedByHttp || preferBrowser || usedBrowser
                        || NeedsBrowserRendering(httpResult.Html)
                        || httpResult.StatusCode is null or >= 500);

                if (!shouldTryBrowser)
                {
                    if (!string.IsNullOrWhiteSpace(httpResult.Html) && !blockedByHttp)
                        return (httpResult.Html, null);

                    return (null, httpResult.Error);
                }

                usedBrowser = true;
                var browserTimeout = blockedByHttp
                    ? Math.Min(_browserPageTimeoutMs, 8_000)
                    : _browserPageTimeoutMs;

                browserSession ??= await BrowserSession.CreateAsync(browserTimeout);
                var browserResult = await browserSession.FetchAsync(url);

                if (!string.IsNullOrWhiteSpace(browserResult.Html))
                    return browserResult;

                if (!string.IsNullOrWhiteSpace(httpResult.Html) && !blockedByHttp)
                    return (httpResult.Html, null);

                return (null, browserResult.Error ?? httpResult.Error);
            }

            async Task<bool> AddPageAsync(string url, bool preferBrowser, bool isProgramDetail = false)
            {
                if (cancellationToken.IsCancellationRequested || IsTimeBudgetExceeded())
                    return false;

                var normalized = NormalizePageUrl(url);
                if (!visited.Add(normalized))
                    return false;

                if (ShouldSkipUrl(normalized))
                    return false;

                var (html, error) = await FetchAsync(normalized, preferBrowser);
                if (string.IsNullOrWhiteSpace(html))
                {
                    if (!string.IsNullOrWhiteSpace(error))
                        errors.Add(error);

                    var isProbeMiss = error?.Contains("404", StringComparison.Ordinal) == true;
                    if (!isProbeMiss)
                        consecutiveFailures++;

                    if (textChunks.Count == 0 && consecutiveFailures >= MaxConsecutiveFetchFailures)
                        throw new InvalidOperationException(
                            "Website blocked scraping (403/401) or returned no readable pages. " +
                            "Try the institute programs listing URL directly, or install Playwright Chromium for browser-based scraping.");

                    return false;
                }

                consecutiveFailures = 0;
                fetchedHtml.Add((normalized, html));
                var text = ExtractPageText(html, isProgramDetail);
                if (!string.IsNullOrWhiteSpace(text))
                    textChunks.Add($"--- Page: {normalized} ---\n{text}");

                return true;
            }

            if (!Uri.TryCreate(homepage, UriKind.Absolute, out _))
                return (null, logoUrl, errors, usedBrowser);

            var listingLimit = _maxListingPages > 0
                ? _maxListingPages
                : (_maxSectionPages > 0 ? _maxSectionPages : 15);

            var listingQueue = new Queue<string>();
            var programUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queuedListing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void EnqueueListing(string url, bool bypassFilter = false)
            {
                var normalized = NormalizePageUrl(url);
                if (visited.Contains(normalized) || queuedListing.Contains(normalized))
                    return;
                if (ShouldSkipUrl(normalized))
                    return;
                if (!bypassFilter && !LooksLikeProgramListingPage(normalized))
                    return;

                queuedListing.Add(normalized);
                listingQueue.Enqueue(normalized);
            }

            EnqueueListing(homepage, bypassFilter: true);
            if (!string.Equals(NormalizePageUrl(websiteUrl), NormalizePageUrl(homepage), StringComparison.OrdinalIgnoreCase))
                EnqueueListing(websiteUrl, bypassFilter: true);

            foreach (var seedUrl in BuildSeedUrls(homepage))
            {
                if (string.Equals(NormalizePageUrl(seedUrl), NormalizePageUrl(homepage), StringComparison.OrdinalIgnoreCase))
                    continue;
                EnqueueListing(seedUrl, bypassFilter: true);
            }

            foreach (var sitemapUrl in await DiscoverSitemapProgramUrlsAsync(homepage, FetchAsync))
            {
                if (LooksLikeProgramDetailUrl(sitemapUrl))
                    programUrls.Add(sitemapUrl);
                else
                    EnqueueListing(sitemapUrl);
            }

            var listingFetched = 0;
            while (listingQueue.Count > 0 && listingFetched < listingLimit)
            {
                if (cancellationToken.IsCancellationRequested || IsTimeBudgetExceeded())
                    break;

                var listingUrl = listingQueue.Dequeue();
                queuedListing.Remove(listingUrl);

                if (!await AddPageAsync(listingUrl, preferBrowser: usedBrowser, isProgramDetail: false))
                    continue;

                listingFetched++;

                var latestPageUrl = fetchedHtml[^1].Url;
                var latestHtml = fetchedHtml[^1].Html;
                foreach (var link in ExtractLinksFromListingPage(latestHtml, latestPageUrl, homepage))
                {
                    if (LooksLikeProgramDetailUrl(link))
                        programUrls.Add(link);
                    else
                        EnqueueListing(link);
                }
            }

            var programList = programUrls
                .Where(url => !visited.Contains(url))
                .OrderBy(url => url, StringComparer.OrdinalIgnoreCase)
                .Take(_maxProgramPages)
                .ToList();

            if (programList.Count > 0)
            {
                using var fetchGate = new SemaphoreSlim(MaxParallelProgramFetches);
                var fetchTasks = programList.Select(async programUrl =>
                {
                    if (cancellationToken.IsCancellationRequested || IsTimeBudgetExceeded())
                        return;

                    await fetchGate.WaitAsync(cancellationToken);
                    try
                    {
                        await AddPageAsync(programUrl, preferBrowser: usedBrowser, isProgramDetail: true);
                    }
                    finally
                    {
                        fetchGate.Release();
                    }
                });

                await Task.WhenAll(fetchTasks);
            }

            if (textChunks.Count == 0)
                return (null, logoUrl, errors, usedBrowser);

            var combined = string.Join("\n\n", textChunks);
            if (_maxCombinedTextLength > 0 && combined.Length > _maxCombinedTextLength)
                combined = combined[.._maxCombinedTextLength];

            logoUrl ??= ExtractLogoUrl(fetchedHtml, homepage);

            return (combined, logoUrl, errors, usedBrowser);
        }
        finally
        {
            if (browserSession is not null)
                await browserSession.DisposeAsync();
        }
    }

    private static bool IsSkippableStatus(int? statusCode) =>
        statusCode is 401 or 403 or 404 or 405 or 410 or 429 or 451;

    private static bool IsSkippableBrowserError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return false;

        return error.Contains("403", StringComparison.Ordinal)
            || error.Contains("401", StringComparison.Ordinal)
            || error.Contains("404", StringComparison.Ordinal)
            || error.Contains("429", StringComparison.Ordinal)
            || error.Contains("(skipped)", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<string>> DiscoverSitemapProgramUrlsAsync(
        string homepage,
        Func<string, bool, Task<(string? Html, string? Error)>> fetchAsync)
    {
        var urls = new List<string>();
        if (!Uri.TryCreate(homepage, UriKind.Absolute, out var baseUri))
            return urls;

        var origin = $"{baseUri.Scheme}://{baseUri.Host}";
        var sitemapCandidates = new[]
        {
            $"{origin}/sitemap.xml",
            $"{origin}/sitemap_index.xml",
            $"{origin}/sitemap-index.xml",
            $"{origin}/wp-sitemap.xml",
        };

        foreach (var sitemapUrl in sitemapCandidates)
        {
            var (html, _) = await fetchAsync(sitemapUrl, false);
            if (string.IsNullOrWhiteSpace(html))
                continue;

            urls.AddRange(ParseSitemapUrls(html, homepage));
            if (urls.Count > 0)
                break;
        }

        return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> ParseSitemapUrls(string xml, string homepage)
    {
        var urls = new List<string>();
        if (!Uri.TryCreate(homepage, UriKind.Absolute, out var baseUri))
            return urls;

        foreach (Match match in Regex.Matches(xml, @"<loc>\s*(?<url>[^<]+)\s*</loc>", RegexOptions.IgnoreCase))
        {
            var url = WebUtility.HtmlDecode(match.Groups["url"].Value.Trim());
            if (string.IsNullOrWhiteSpace(url))
                continue;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                continue;

            if (!IsAllowedHost(uri.Host, baseUri.Host))
                continue;

            if (ShouldSkipUrl(url))
                continue;

            if (!LooksLikeProgramDetailUrl(url) && !LooksLikeProgramListingPage(url))
                continue;

            urls.Add(NormalizePageUrl(url));
        }

        return urls;
    }

    private static IEnumerable<string> ExtractLinksFromListingPage(string html, string pageUrl, string siteHomepage)
    {
        foreach (var link in ExtractAllInternalLinks(html, pageUrl, siteHomepage))
        {
            if (LooksLikeProgramDetailUrl(link) || LooksLikeProgramListingPage(link) || LooksLikePaginationLink(link))
                yield return link;
        }
    }

    private static bool LooksLikeProgramListingPage(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || ShouldSkipUrl(url) || IsIrrelevantCrawlPath(url))
            return false;

        if (LooksLikeProgramDetailUrl(url))
            return false;

        var value = url.ToLowerInvariant();

        if (LooksLikePaginationLink(value))
            return true;

        return value.Contains("/academics/programs")
            || value.EndsWith("/programs", StringComparison.Ordinal)
            || value.Contains("/programs?")
            || value.Contains("/find-a-course")
            || value.Contains("/explore-our-programs")
            || value.Contains("collection=und-sp-program")
            || value.EndsWith("/degree-programs", StringComparison.Ordinal)
            || value.Contains("/courses/")
            || value.EndsWith("/courses", StringComparison.Ordinal)
            || value.EndsWith("/search", StringComparison.Ordinal)
            || value.EndsWith("/search/", StringComparison.Ordinal)
            || value.Contains("/study-with-us/explore-our-programs");
    }

    private static bool IsIrrelevantCrawlPath(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return true;

        var host = uri.Host.ToLowerInvariant();
        var value = url.ToLowerInvariant();

        if (host.StartsWith("news.", StringComparison.Ordinal)
            || host.StartsWith("podcast.", StringComparison.Ordinal)
            || host.StartsWith("mobile.", StringComparison.Ordinal)
            || host.Contains("fightingirish", StringComparison.Ordinal))
        {
            return true;
        }

        if (host.Contains("graduateschool.", StringComparison.Ordinal)
            && !value.Contains("/degree-programs/", StringComparison.Ordinal))
        {
            return true;
        }

        if (value.Contains("/graduate/", StringComparison.Ordinal)
            && !value.Contains("/degree-programs/", StringComparison.Ordinal))
        {
            return true;
        }

        string[] blockedFragments =
        {
            "/our-experts/", "/experts/", "/news/", "/stories/", "/story/",
            "/events/", "/event/", "/athletics/", "/giving/", "/alumni/",
            "/magazine/", "/podcast/", "/careers/", "/jobs/", "/employment/",
            "/faculty-staff/", "/faculty/", "/staff-directory/", "/people/",
            "/multimedia/", "/video/", "/photos/", "/galleries/",
            "/press-release/", "/in-the-news/", "/obituaries/",
            "/campus-news/", "/research-news/", "/experts-directory/",
            "/category/", "/tag/", "/author/", "/archive/",
            "/grants/", "/opportunities/", "/admissions/",
            "/program-of-study/", "/student-opportunities/", "/placements/",
            "/fellowship", "/dissertation", "/financial-support",
            "/graduate-training/", "/intellectual-community", "/favicon.ico",
        };

        return blockedFragments.Any(fragment => value.Contains(fragment, StringComparison.Ordinal));
    }

    private static bool LooksLikeProgramDetailUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || ShouldSkipUrl(url) || IsIrrelevantCrawlPath(url))
            return false;

        var value = url.ToLowerInvariant();
        var segments = value.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (value.Contains("/degree-programs/") && segments.Length >= 4)
            return true;

        if (value.Contains("/programs/") && segments.Length >= 5)
            return true;

        if (value.Contains("/program/") && segments.Length >= 4)
            return true;

        if (value.Contains("/courses/") && segments.Length >= 4)
            return true;

        if (value.Contains("/study-with-us/") && segments.Length >= 4)
            return true;

        return value.Contains("/bachelor-of")
            || value.Contains("/master-of")
            || value.Contains("/associate-degree")
            || value.Contains("/doctor-of")
            || value.Contains("/phd-in")
            || value.Contains("/diploma-of")
            || value.Contains("/certificate-in")
            || value.Contains("/majors/");
    }

    private static bool LooksLikePaginationLink(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return false;

        var value = href.ToLowerInvariant();
        return value.Contains("page=")
            || value.Contains("/page/")
            || value.Contains("start_rank=")
            || value.Contains("offset=")
            || Regex.IsMatch(value, @"[?&]p=\d+");
    }

    private static IEnumerable<string> ExtractAllInternalLinks(string html, string pageUrl, string siteHomepage)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri))
            yield break;

        if (!Uri.TryCreate(siteHomepage, UriKind.Absolute, out var baseUri))
            yield break;

        var origin = $"{pageUri.Scheme}://{pageUri.Host}";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var linkPattern = new Regex(
            @"href=[""'](?<href>[^""'#]+)[""']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (Match match in linkPattern.Matches(html))
        {
            var href = WebUtility.HtmlDecode(match.Groups["href"].Value.Trim());
            if (string.IsNullOrWhiteSpace(href)
                || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var absolute = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? href.Split('#')[0]
                : $"{origin}{href.Split('#')[0]}";

            if (!Uri.TryCreate(absolute, UriKind.Absolute, out var linkUri))
                continue;

            if (!IsAllowedHost(linkUri.Host, baseUri.Host))
                continue;

            if (ShouldSkipUrl(absolute))
                continue;

            var normalized = NormalizePageUrl(absolute);
            if (seen.Add(normalized))
                yield return normalized;
        }
    }

    private static IEnumerable<string> BuildSeedUrls(string homepage)
    {
        if (!Uri.TryCreate(homepage, UriKind.Absolute, out var uri))
            yield break;

        var origin = $"{uri.Scheme}://{uri.Host}";
        var host = uri.Host.ToLowerInvariant();

        yield return homepage;

        if (host.Contains("boxhill.edu.au", StringComparison.OrdinalIgnoreCase)
            || host.Contains("bhtafe.edu.au", StringComparison.OrdinalIgnoreCase))
        {
            yield return $"{origin}/search/";
            yield return $"{origin}/courses/undergraduate/";
            yield return $"{origin}/courses/postgraduate/";
            yield return $"{origin}/courses/short-courses/";
            yield return $"{origin}/courses/vet/";
        }

        foreach (var suffix in CommonStudyPaths)
            yield return $"{origin}{suffix}";

        if (host.Contains("notredame.edu.au"))
        {
            yield return "https://search.nd.edu.au/s/search.html?collection=und-sp-program";
            yield return "https://search.nd.edu.au/";
        }

        if (host.EndsWith("nd.edu", StringComparison.OrdinalIgnoreCase))
        {
            yield return "https://www.nd.edu/academics/programs/";
            yield return "https://graduateschool.nd.edu/degree-programs/";
        }
    }

    private static bool ShouldSkipUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        var value = url.ToLowerInvariant();
        if (value.Contains("login")
            || value.Contains("logout")
            || value.Contains("signin")
            || value.Contains("sign-in")
            || value.Contains("signup")
            || value.Contains("sign-up")
            || value.Contains("wp-admin")
            || value.Contains("wp-login")
            || value.Contains("cart")
            || value.Contains("checkout")
            || value.Contains("/feed")
            || value.Contains("xmlrpc"))
        {
            return true;
        }

        return value.EndsWith(".pdf")
            || value.EndsWith(".jpg")
            || value.EndsWith(".jpeg")
            || value.EndsWith(".png")
            || value.EndsWith(".gif")
            || value.EndsWith(".webp")
            || value.EndsWith(".svg")
            || value.EndsWith(".zip")
            || value.EndsWith(".doc")
            || value.EndsWith(".docx")
            || value.EndsWith(".xls")
            || value.EndsWith(".xlsx")
            || value.EndsWith(".css")
            || value.EndsWith(".js");
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

    private static readonly string[] CommonStudyPaths =
    {
        "/search",
        "/search/",
        "/courses/undergraduate/",
        "/courses/postgraduate/",
        "/courses/short-courses/",
        "/courses/vet/",
        "/courses/higher-education/",
        "/course-search/",
        "/study-with-us",
        "/study-with-us/explore-our-programs",
        "/study-with-us/explore-our-programs/undergraduate",
        "/study-with-us/explore-our-programs/postgraduate",
        "/study",
        "/courses",
        "/programs",
        "/find-a-course",
        "/international-students",
        "/academics/programs",
        "/academics/programs/",
    };

    private static bool IsAllowedHost(string linkHost, string baseHost)
    {
        if (string.Equals(linkHost, baseHost, StringComparison.OrdinalIgnoreCase))
            return true;

        var blocked = new[] { "facebook.", "twitter.", "instagram.", "linkedin.", "youtube.", "google.", "apple.com", "microsoft.com" };
        if (blocked.Any(fragment => linkHost.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            return false;

        var baseParts = baseHost.Split('.');
        if (baseParts.Length >= 2)
        {
            var registrableDomain = string.Join('.', baseParts[^2..]);
            if (linkHost.EndsWith(registrableDomain, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private async Task<(string? Html, string? Error, int? StatusCode)> TryFetchHtmlWithHttpAsync(string url)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("InstituteScraper");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Referer", url);

            using var response = await client.SendAsync(request);
            var statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
                return (null, $"{url} returned {statusCode} {response.ReasonPhrase}", statusCode);

            var html = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(html)
                ? (null, $"{url} returned empty content.", statusCode)
                : (html, null, statusCode);
        }
        catch (Exception ex)
        {
            return (null, $"{url}: {ex.Message}", null);
        }
    }

    private string ExtractPageText(string html, bool isProgramDetail)
    {
        var chunks = new List<string>();

        foreach (Match match in Regex.Matches(
            html,
            @"<script[^>]+id=""__NEXT_DATA__""[^>]*>(?<json>[\s\S]*?)</script>",
            RegexOptions.IgnoreCase))
        {
            var condensed = CondenseEducationJson(match.Groups["json"].Value);
            if (!string.IsNullOrWhiteSpace(condensed))
                chunks.Add($"--- Embedded page data ---\n{condensed}");
        }

        foreach (Match match in Regex.Matches(
            html,
            @"<script[^>]+type=""application/ld\+json""[^>]*>(?<json>[\s\S]*?)</script>",
            RegexOptions.IgnoreCase))
        {
            var json = match.Groups["json"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(json))
                chunks.Add($"--- Structured data ---\n{TruncateText(json, 15_000)}");
        }

        var visible = ExtractVisibleText(html, isProgramDetail);
        if (!string.IsNullOrWhiteSpace(visible))
            chunks.Add(visible);

        if (chunks.Count == 0)
            return string.Empty;

        var combined = string.Join("\n\n", chunks);
        var hasEmbeddedData = chunks.Any(chunk => chunk.StartsWith("--- Embedded page data ---", StringComparison.Ordinal));
        var limit = isProgramDetail
            ? _maxPageTextLength
            : hasEmbeddedData
                ? Math.Min(_maxPageTextLength, 120_000)
                : Math.Min(_maxPageTextLength, 8000);

        if (_maxPageTextLength <= 0)
            return combined;

        return combined.Length > limit ? combined[..limit] : combined;
    }

    private static string CondenseEducationJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        string[] patterns =
        [
            @"""CourseName""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""courseName""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""programName""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""ProgramName""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""name""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""Slug""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""slug""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""CRICOS[^""]*""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""cricos[^""]*""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""Duration""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""duration""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""Intake""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""intake""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""CourseLevel""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""level""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""Fees[^""]*""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""fees[^""]*""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""english[^""]*""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""Description""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""description""\s*:\s*""(?<value>(?:\\.|[^""\\])*)""",
            @"""url""\s*:\s*""(?<value>https?://[^""\\]+)""",
        ];

        var lines = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in patterns)
        {
            foreach (Match match in Regex.Matches(json, pattern, RegexOptions.IgnoreCase))
            {
                var label = match.Value.Split(':')[0].Trim().Trim('"');
                var value = WebUtility.HtmlDecode(match.Groups["value"].Value.Replace("\\\"", "\""));
                if (string.IsNullOrWhiteSpace(value) || value.Length > 500)
                    continue;

                var line = $"{label}: {value}";
                if (seen.Add(line))
                    lines.Add(line);
            }
        }

        var result = string.Join("\n", lines);
        return result.Length > 120_000 ? result[..120_000] : result;
    }

    private static bool HasUsableHttpContent(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return false;

        foreach (Match match in Regex.Matches(
            html,
            @"<script[^>]+id=""__NEXT_DATA__""[^>]*>(?<json>[\s\S]*?)</script>",
            RegexOptions.IgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(CondenseEducationJson(match.Groups["json"].Value)))
                return true;
        }

        if (html.Contains("application/ld+json", StringComparison.OrdinalIgnoreCase))
            return true;

        var withoutScripts = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
        var withoutStyles = Regex.Replace(withoutScripts, @"<style[^>]*>[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);
        var withoutTags = Regex.Replace(withoutStyles, "<[^>]+>", " ");
        var visible = Regex.Replace(WebUtility.HtmlDecode(withoutTags), @"\s+", " ").Trim();

        return visible.Length >= 500;
    }

    private static bool NeedsBrowserRendering(string html)
    {
        if (string.IsNullOrWhiteSpace(html) || html.Length < 5000)
            return false;

        var hasNextShell = html.Contains("__NEXT_DATA__", StringComparison.Ordinal)
            || html.Contains("id=\"__next\"", StringComparison.OrdinalIgnoreCase)
            || html.Contains("id='__next'", StringComparison.OrdinalIgnoreCase);

        if (!hasNextShell)
            return false;

        var withoutScripts = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
        var withoutStyles = Regex.Replace(withoutScripts, @"<style[^>]*>[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);
        var withoutTags = Regex.Replace(withoutStyles, "<[^>]+>", " ");
        var visible = Regex.Replace(WebUtility.HtmlDecode(withoutTags), @"\s+", " ").Trim();

        return visible.Length < 500;
    }

    private static string TruncateText(string text, int maxChars) =>
        text.Length > maxChars ? text[..maxChars] : text;

    private string ExtractVisibleText(string html, bool isProgramDetail)
    {
        var withoutScripts = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
        var withoutStyles = Regex.Replace(withoutScripts, @"<style[^>]*>[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);
        var withoutTags = Regex.Replace(withoutStyles, "<[^>]+>", " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        var normalized = Regex.Replace(decoded, @"\s+", " ").Trim();

        if (_maxPageTextLength <= 0)
            return normalized;

        var limit = isProgramDetail ? _maxPageTextLength : Math.Min(_maxPageTextLength, 8000);
        return normalized.Length > limit ? normalized[..limit] : normalized;
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

        if (alt.Contains("logo"))
            score += 90;

        if (cls.Contains("logo") || cls.Contains("site-logo") || cls.Contains("brand"))
            score += 80;

        if (url.Contains("logo") || url.Contains("crest") || url.Contains("brand"))
            score += 60;

        if (url.Contains("pixel") || url.Contains("analytics") || url.Contains("banner"))
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
        private readonly int _pageTimeoutMs;

        private BrowserSession(IPlaywright playwright, IBrowser browser, IBrowserContext context, int pageTimeoutMs)
        {
            _playwright = playwright;
            _browser = browser;
            _context = context;
            _pageTimeoutMs = pageTimeoutMs;
        }

        public static async Task<BrowserSession> CreateAsync(int pageTimeoutMs)
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

            return new BrowserSession(playwright, browser, context, pageTimeoutMs);
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
                        Timeout = _pageTimeoutMs,
                    });

                    if (response is not null && IsSkippableStatus((int)response.Status))
                        return (null, $"{url} returned {(int)response.Status} via browser (skipped)");

                    await page.WaitForTimeoutAsync(500);

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
            catch (TimeoutException)
            {
                return (null, $"{url} (browser): timed out after {_pageTimeoutMs}ms (skipped)");
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
                return "Browser not installed. Run playwright.ps1 install chromium, then restart the API.";

            var boxIndex = message.IndexOf('╔', StringComparison.Ordinal);
            if (boxIndex > 0)
                message = message[..boxIndex];

            return message.Trim();
        }
    }
}
