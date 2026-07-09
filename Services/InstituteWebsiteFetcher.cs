using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.InstituteScrapping;
using Microsoft.Playwright;

namespace AvecADeskApi.Services;

public class InstituteWebsiteFetcher : IInstituteWebsiteFetcher
{
    private const int MaxSectionPages = 15;
    private const int MaxProgramDetailPages = 250;

    private readonly IHttpClientFactory _httpClientFactory;

    public InstituteWebsiteFetcher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(string? CombinedText, string? LogoUrl, List<string> Errors, bool UsedBrowser, List<ScrapedProgramRecord> ParsedPrograms)> FetchWebsiteTextAsync(string websiteUrl)
    {
        var homepage = NormalizeHomepageUrl(websiteUrl);
        var errors = new List<string>();
        var textChunks = new List<string>();
        var fetchedHtml = new List<(string Url, string Html)>();
        var parsedPrograms = new List<ScrapedProgramRecord>();
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
                parsedPrograms.AddRange(ExtractProgramsFromHtml(html, normalized));
                var text = ExtractPageText(html);
                if (!string.IsNullOrWhiteSpace(text))
                    textChunks.Add($"--- Page: {normalized} ---\n{text}");

                return true;
            }

            await AddPageAsync(homepage, preferBrowser: false);

            if (Uri.TryCreate(homepage, UriKind.Absolute, out var homeUri))
            {
                var listingPaths = CommonStudyPaths
                    .Where(path => path.Contains("program", StringComparison.OrdinalIgnoreCase)
                        || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                        || path.Contains("academic", StringComparison.OrdinalIgnoreCase))
                    .Select(path => NormalizePageUrl($"{homeUri.Scheme}://{homeUri.Host}{path}"))
                    .Where(url => !visited.Contains(url))
                    .ToList();

                foreach (var listingUrl in listingPaths)
                    await AddPageAsync(listingUrl, preferBrowser: usedBrowser);
            }

            var sectionUrls = DiscoverSectionUrls(homepage, fetchedHtml)
                .Take(MaxSectionPages)
                .ToList();

            foreach (var sectionUrl in sectionUrls)
                await AddPageAsync(sectionUrl, preferBrowser: usedBrowser);

            var catalogPrograms = DeduplicatePrograms(parsedPrograms);
            var detailUrls = catalogPrograms
                .Where(program => !string.IsNullOrWhiteSpace(program.ProgramLink))
                .Select(program => NormalizePageUrl(program.ProgramLink!))
                .Where(url => !visited.Contains(url))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxProgramDetailPages)
                .ToList();

            foreach (var detailUrl in detailUrls)
                await AddPageAsync(detailUrl, preferBrowser: usedBrowser);

            EnrichProgramsFromDetailPages(catalogPrograms, fetchedHtml);

            if (textChunks.Count == 0)
                return (null, logoUrl, errors, usedBrowser, catalogPrograms);

            var combined = string.Join("\n\n", textChunks);
            if (combined.Length > 180000)
                combined = combined[..180000];

            logoUrl ??= ExtractLogoUrl(fetchedHtml, homepage);

            return (combined, logoUrl, errors, usedBrowser, catalogPrograms);
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

        foreach (var url in ExtractEmbeddedPathUrls(pages, origin, @"/study-areas/[a-z0-9\-]+/?"))
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
        "/academics/programs",
        "/academics",
        "/find-a-course",
        "/international-students",
        "/international-students/international-courses",
        "/about-us",
        "/courses/undergraduate",
        "/courses/short-courses",
        "/search",
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
            || value.Contains("study-areas")
            || value.Contains("about-us")
            || value.Contains("academics")
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
            || value.Contains("/study-areas/")
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

    private static IEnumerable<string> ExtractEmbeddedProgramUrls(
        IEnumerable<(string Url, string Html)> pages,
        string websiteUrl)
    {
        return ExtractEmbeddedPathUrls(pages, websiteUrl, @"/courses/[a-z0-9][a-z0-9\-]+/?")
            .Where(url =>
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    return false;

                var path = uri.AbsolutePath.TrimEnd('/');
                if (IsCourseListingPath(path))
                    return false;

                var slug = path["/courses/".Length..];
                return slug.Length >= 12;
            });
    }

    private static IEnumerable<string> ExtractEmbeddedPathUrls(
        IEnumerable<(string Url, string Html)> pages,
        string websiteUrl,
        string pathPattern)
    {
        if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var baseUri))
            yield break;

        var origin = $"{baseUri.Scheme}://{baseUri.Host}";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var regex = new Regex($@"(?<path>{pathPattern})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (var (_, html) in pages)
        {
            foreach (Match match in regex.Matches(html))
            {
                var path = match.Groups["path"].Value.TrimEnd('/');
                var absolute = NormalizePageUrl($"{origin}{path}");
                if (seen.Add(absolute))
                    yield return absolute;
            }
        }
    }

    private static bool IsCourseListingPath(string path)
    {
        var normalized = path.TrimEnd('/').ToLowerInvariant();
        return normalized is "/courses"
            or "/courses/undergraduate"
            or "/courses/short-courses"
            or "/courses/study-options"
            or "/courses/apprenticeships"
            or "/courses/pre-apprenticeships"
            or "/courses/search";
    }

    private static string ExtractPageText(string html)
    {
        var visible = ExtractVisibleText(html);
        var embedded = ExtractEmbeddedJsonText(html);

        if (string.IsNullOrWhiteSpace(embedded))
            return visible;

        if (string.IsNullOrWhiteSpace(visible))
            return embedded;

        return $"{visible}\n\n--- Embedded page data ---\n{embedded}";
    }

    private static string? ExtractEmbeddedJsonText(string html)
    {
        var match = Regex.Match(
            html,
            @"<script[^>]+id=[""']__NEXT_DATA__[""'][^>]*>(?<json>.*?)</script>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!match.Success)
            return null;

        try
        {
            using var document = JsonDocument.Parse(match.Groups["json"].Value);
            var flattened = FlattenJsonToText(document.RootElement);
            return string.IsNullOrWhiteSpace(flattened)
                ? null
                : flattened.Length > 25000 ? flattened[..25000] : flattened;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string FlattenJsonToText(JsonElement element)
    {
        var builder = new StringBuilder();
        var skipKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "__typename", "_modelApiKey", "buildId", "md5", "blurhash", "blurUpThumb",
            "mimeType", "basename", "filename", "focalPoint", "exifInfo", "customData",
            "width", "height", "size", "format", "tags", "smartTags", "colors",
            "createdAt", "updatedAt", "reviewDate", "isFallback", "gsp", "appGip",
            "scriptLoader", "query", "faviconMetaTags", "_seoMetaTags", "allNavigationLinks",
            "v2FooterLinks", "sitePopups", "preHeaderBanner", "fourColBlock",
        };

        WalkJson(element, builder, skipKeys, depth: 0);
        return builder.ToString().Trim();
    }

    private static void WalkJson(
        JsonElement element,
        StringBuilder builder,
        HashSet<string> skipKeys,
        int depth,
        string? currentKey = null)
    {
        if (depth > 12)
            return;

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (skipKeys.Contains(property.Name))
                        continue;

                    WalkJson(property.Value, builder, skipKeys, depth + 1, property.Name);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    WalkJson(item, builder, skipKeys, depth + 1, currentKey);
                break;

            case JsonValueKind.String:
                var value = element.GetString();
                if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
                    break;

                if (ShouldIncludeJsonString(currentKey, value))
                {
                    if (!string.IsNullOrWhiteSpace(currentKey))
                        builder.AppendLine($"{currentKey}: {value.Trim()}");
                    else
                        builder.AppendLine(value.Trim());
                }
                break;
        }
    }

    private static bool ShouldIncludeJsonString(string? key, string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return false;

        if (key is not null && InterestingJsonKeys.Contains(key))
            return true;

        if (trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return key is "url" or "programLink" or "logo" or "programLogo";

        if (trimmed.StartsWith('/') && trimmed.Count(c => c == '/') <= 4)
            return key is "slug" or "path" or "pagePath";

        return trimmed.Length >= 20
            && !Regex.IsMatch(trimmed, @"^[a-f0-9\-]{20,}$", RegexOptions.IgnoreCase);
    }

    private static readonly HashSet<string> InterestingJsonKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "title", "name", "programName", "courseName", "fullTitle", "heading", "subheading",
        "description", "summary", "about", "boxHillDescription", "content", "message",
        "cricos", "cricosCode", "duration", "intake", "feesYearly", "englishReq",
        "country", "city", "state", "campus", "level", "qualification", "scholarshipsDetails",
        "programDescription", "addmissionRequirements", "admissionRequirements", "eligibility",
        "entryRequirements", "requirements", "notes", "seoSettings", "mailingAddress",
        "footerAcknowledgement", "contactDescription", "courseEnquiryFormDescription",
    };

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

        foreach (var (pageUrl, html) in pages)
        {
            if (string.IsNullOrWhiteSpace(html))
                continue;

            if (!IsSameSiteHost(pageUrl, homepage))
                continue;

            var pageOrigin = Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)
                ? $"{pageUri.Scheme}://{pageUri.Host}"
                : origin;
            var onHomepage = IsHomepageUrl(pageUrl, homepage);

            foreach (Match match in Regex.Matches(html, @"<img\b[^>]*>", RegexOptions.IgnoreCase))
            {
                var tag = match.Value;
                var absolute = ResolveImgSrc(tag, pageOrigin);
                if (string.IsNullOrWhiteSpace(absolute))
                    continue;

                var score = ScoreLogoCandidate(tag, absolute, onHomepage);
                if (score > 0)
                    scored.Add((score, absolute));
            }

            if (!onHomepage)
                continue;

            foreach (Match match in Regex.Matches(
                html,
                @"<meta[^>]+(?:property=[""']og:image[""']|name=[""']twitter:image[""'])[^>]+content=[""'](?<url>[^""']+)[""']|<meta[^>]+content=[""'](?<url>[^""']+)[""'][^>]+(?:property=[""']og:image[""']|name=[""']twitter:image[""'])",
                RegexOptions.IgnoreCase))
            {
                var absolute = ToAbsoluteUrl(match.Groups["url"].Value, pageOrigin);
                if (!string.IsNullOrWhiteSpace(absolute) && !IsLikelyNonBrandLogo(string.Empty, string.Empty, absolute))
                    scored.Add((25, absolute));
            }

            foreach (Match match in Regex.Matches(
                html,
                @"<link[^>]+rel=[""']mask-icon[""'][^>]+href=[""'](?<url>[^""']+)[""']|<link[^>]+href=[""'](?<url>[^""']+)[""'][^>]+rel=[""']mask-icon[""']",
                RegexOptions.IgnoreCase))
            {
                var absolute = ToAbsoluteUrl(match.Groups["url"].Value, pageOrigin);
                if (!string.IsNullOrWhiteSpace(absolute))
                    scored.Add((180, absolute));
            }

            foreach (Match match in Regex.Matches(
                html,
                @"<link[^>]+rel=[""'](?:apple-touch-icon|icon|shortcut icon)[""'][^>]+href=[""'](?<url>[^""']+)[""']|<link[^>]+href=[""'](?<url>[^""']+)[""'][^>]+rel=[""'](?:apple-touch-icon|icon|shortcut icon)[""']",
                RegexOptions.IgnoreCase))
            {
                var absolute = ToAbsoluteUrl(match.Groups["url"].Value, pageOrigin);
                if (string.IsNullOrWhiteSpace(absolute))
                    continue;

                var iconScore = absolute.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ? 15 : 40;
                if (absolute.Contains("monogram", StringComparison.OrdinalIgnoreCase)
                    || absolute.Contains("crest", StringComparison.OrdinalIgnoreCase)
                    || absolute.Contains("emblem", StringComparison.OrdinalIgnoreCase))
                {
                    iconScore += 40;
                }

                scored.Add((iconScore, absolute));
            }
        }

        return scored
            .OrderByDescending(item => item.Score)
            .Select(item => item.Url)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string? ResolveImgSrc(string imgTag, string origin)
    {
        var src = ReadAttribute(imgTag, "src");
        var lazySrc = ReadAttribute(imgTag, "data-lazy-src")
            ?? ReadAttribute(imgTag, "data-src")
            ?? ReadAttribute(imgTag, "data-original");

        if (IsPlaceholderImageSrc(src) && !string.IsNullOrWhiteSpace(lazySrc))
            src = lazySrc;

        if (IsPlaceholderImageSrc(src))
            return null;

        return ToAbsoluteUrl(src!, origin);
    }

    private static bool IsPlaceholderImageSrc(string? src)
    {
        if (string.IsNullOrWhiteSpace(src))
            return true;

        var value = src.Trim();
        return value.StartsWith("data:image", StringComparison.OrdinalIgnoreCase)
            || value == "#";
    }

    private static bool IsHomepageUrl(string pageUrl, string homepage)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)
            || !Uri.TryCreate(homepage, UriKind.Absolute, out var homeUri))
        {
            return false;
        }

        return string.Equals(NormalizeHost(pageUri.Host), NormalizeHost(homeUri.Host), StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrEmpty(pageUri.AbsolutePath) || pageUri.AbsolutePath == "/");
    }

    private static bool IsSameSiteHost(string pageUrl, string homepage)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)
            || !Uri.TryCreate(homepage, UriKind.Absolute, out var homeUri))
        {
            return false;
        }

        return string.Equals(NormalizeHost(pageUri.Host), NormalizeHost(homeUri.Host), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeHost(string host)
    {
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
    }

    private static bool IsLikelyNonBrandLogo(string alt, string cls, string url)
    {
        var combined = $"{alt} {cls} {url}".ToLowerInvariant();
        string[] blockedTokens =
        [
            "teqsa",
            "accreditation",
            "accredited-by",
            "accredited_by",
            "partner-logo",
            "member-logo",
            "ihea",
            "aipm",
            "wallet-hub",
            "wallet_hub",
            "endorsed",
            "award-",
            "badge",
            "/seal",
            "sponsor",
            "ranking",
        ];

        return blockedTokens.Any(token => combined.Contains(token, StringComparison.Ordinal));
    }

    private static bool ContainsLogoToken(string value)
    {
        return Regex.IsMatch(value, @"(?:^|[\s_\-])logo(?:$|[\s_\-])", RegexOptions.IgnoreCase);
    }

    private static int ScoreLogoCandidate(string imgTag, string absoluteUrl, bool onHomepage)
    {
        var tag = imgTag.ToLowerInvariant();
        var url = absoluteUrl.ToLowerInvariant();
        var score = 0;

        var alt = ReadAttribute(imgTag, "alt")?.ToLowerInvariant() ?? string.Empty;
        var cls = ReadAttribute(imgTag, "class")?.ToLowerInvariant() ?? string.Empty;
        var id = ReadAttribute(imgTag, "id")?.ToLowerInvariant() ?? string.Empty;

        if (IsLikelyNonBrandLogo(alt, cls, url))
            return 0;

        if (onHomepage)
            score += 100;

        if (Regex.IsMatch(cls, @"\b(header-logo|site-logo|brand-logo|logo-img|nav-logo|navbar-logo)\b", RegexOptions.IgnoreCase))
            score += 150;
        else if (Regex.IsMatch(cls, @"header.*logo|logo.*header", RegexOptions.IgnoreCase))
            score += 120;
        else if (ContainsLogoToken(cls) || cls.Contains("top-nav") || cls.Contains("nav__img") || cls.Contains("site-logo") || cls.Contains("brand"))
            score += 80;

        if (alt is "logo" or "site logo" or "institute logo" or "university logo")
            score += 120;
        else if (ContainsLogoToken(alt) || alt.Contains("college", StringComparison.Ordinal) || alt.Contains("university", StringComparison.Ordinal))
            score += 70;

        if (ContainsLogoToken(id) || id.Contains("brand"))
            score += 70;

        if (ContainsLogoToken(url) || url.Contains("crest") || url.Contains("brand") || url.Contains("emblem") || url.Contains("monogram"))
            score += 60;

        if (url.EndsWith(".svg") || url.EndsWith(".png") || url.EndsWith(".webp"))
            score += 15;

        if (url.EndsWith(".ico"))
            score -= 30;

        if (tag.Contains("header") || cls.Contains("header"))
            score += 25;

        if (url.Contains("pixel") || url.Contains("analytics") || url.Contains("banner") || url.Contains("hero"))
            score -= 40;

        if (!onHomepage)
            score -= 50;

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

    private static List<ScrapedProgramRecord> ExtractProgramsFromHtml(string html, string pageUrl)
    {
        var programs = new List<ScrapedProgramRecord>();
        if (string.IsNullOrWhiteSpace(html))
            return programs;

        var origin = Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)
            ? $"{pageUri.Scheme}://{pageUri.Host}"
            : null;

        var listItemPattern = new Regex(
            @"<li\b[^>]*(?:data-query-item|class=""[^""]*card-container[^""]*"")[^>]*>(?<item>.*?)</li>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (Match match in listItemPattern.Matches(html))
        {
            var item = match.Groups["item"].Value;
            var programName = ExtractRegexGroup(
                item,
                @"<h[1-6][^>]*class=""[^""]*card-title[^""]*""[^>]*>.*?<a[^>]*>(?<name>[^<]+)</a>",
                "name");

            if (string.IsNullOrWhiteSpace(programName))
                continue;

            var programLink = ExtractRegexGroup(
                item,
                @"<a[^>]*class=""[^""]*card-link[^""]*""[^>]*href=""(?<link>[^""]+)""",
                "link")
                ?? ExtractRegexGroup(
                    item,
                    @"<h[1-6][^>]*class=""[^""]*card-title[^""]*""[^>]*>.*?<a[^>]*href=""(?<link>[^""]+)""",
                    "link");

            var meta = ExtractRegexGroup(
                item,
                @"<p[^>]*class=""[^""]*card-meta[^""]*""[^>]*>(?<meta>[^<]+)</p>",
                "meta");

            var level = ExtractRegexGroup(match.Value, @"data-type=""(?<type>[^""]+)""", "type");
            var campus = meta;

            if (!string.IsNullOrWhiteSpace(meta) && meta.Contains(" - ", StringComparison.Ordinal))
            {
                var parts = meta.Split(" - ", 2, StringSplitOptions.TrimEntries);
                level = FirstNonEmptyString(level, parts[0]);
                campus = parts.Length > 1 ? parts[1] : meta;
            }
            else
            {
                level = FirstNonEmptyString(level, meta);
            }

            programs.Add(new ScrapedProgramRecord
            {
                ProgramName = WebUtility.HtmlDecode(programName.Trim()),
                ProgramLink = NormalizeProgramLink(programLink, origin),
                Level = NormalizeLevel(level),
                Campus = string.IsNullOrWhiteSpace(campus) ? null : WebUtility.HtmlDecode(campus.Trim()),
            });
        }

        if (programs.Count == 0)
        {
            var cardPattern = new Regex(
                @"<h[1-6][^>]*class=""[^""]*card-title[^""]*""[^>]*>.*?<a[^>]*href=""(?<link>[^""]+)""[^>]*>(?<name>[^<]+)</a>.*?<p[^>]*class=""[^""]*card-meta[^""]*""[^>]*>(?<meta>[^<]+)</p>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            foreach (Match match in cardPattern.Matches(html))
            {
                var meta = WebUtility.HtmlDecode(match.Groups["meta"].Value.Trim());
                var level = meta;
                var campus = meta;

                if (meta.Contains(" - ", StringComparison.Ordinal))
                {
                    var parts = meta.Split(" - ", 2, StringSplitOptions.TrimEntries);
                    level = parts[0];
                    campus = parts.Length > 1 ? parts[1] : meta;
                }

                programs.Add(new ScrapedProgramRecord
                {
                    ProgramName = WebUtility.HtmlDecode(match.Groups["name"].Value.Trim()),
                    ProgramLink = NormalizeProgramLink(match.Groups["link"].Value, origin),
                    Level = NormalizeLevel(level),
                    Campus = campus,
                });
            }
        }

        return programs;
    }

    private static List<ScrapedProgramRecord> DeduplicatePrograms(IEnumerable<ScrapedProgramRecord> programs)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<ScrapedProgramRecord>();

        foreach (var program in programs)
        {
            if (string.IsNullOrWhiteSpace(program.ProgramName))
                continue;

            var key = !string.IsNullOrWhiteSpace(program.ProgramLink)
                ? program.ProgramLink.Trim().TrimEnd('/').ToLowerInvariant()
                : program.ProgramName.Trim().ToLowerInvariant();

            if (!seen.Add(key))
                continue;

            results.Add(program);
        }

        return results;
    }

    private static void EnrichProgramsFromDetailPages(
        IReadOnlyList<ScrapedProgramRecord> programs,
        IReadOnlyList<(string Url, string Html)> fetchedHtml)
    {
        var htmlByUrl = fetchedHtml
            .GroupBy(page => NormalizePageUrl(page.Url), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Html, StringComparer.OrdinalIgnoreCase);

        foreach (var program in programs)
        {
            if (string.IsNullOrWhiteSpace(program.ProgramLink))
                continue;

            if (!htmlByUrl.TryGetValue(NormalizePageUrl(program.ProgramLink), out var html))
                continue;

            program.ProgramLogo = FirstNonEmptyString(
                program.ProgramLogo,
                ExtractProgramHeroImage(html, program.ProgramLink));

            program.ProgramDescription = FirstNonEmptyString(
                program.ProgramDescription,
                ExtractProgramDescription(html));

            program.AddmissionRequirements = FirstNonEmptyString(
                program.AddmissionRequirements,
                ExtractAdmissionRequirements(html));
        }
    }

    private static string? ExtractProgramHeroImage(string html, string pageUrl)
    {
        var origin = Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)
            ? $"{pageUri.Scheme}://{pageUri.Host}"
            : null;

        var candidates = new[]
        {
            ExtractRegexGroup(html, @"property=""og:image""[^>]*content=""(?<url>[^""]+)""", "url"),
            ExtractRegexGroup(html, @"content=""(?<url>[^""]+)""[^>]*property=""og:image""", "url"),
            ExtractRegexGroup(html, @"<div[^>]*class=""[^""]*hero[^""]*""[^>]*>.*?<img[^>]*src=""(?<url>[^""]+)""", "url"),
            ExtractRegexGroup(html, @"<img[^>]*class=""[^""]*hero-desktop[^""]*""[^>]*src=""(?<url>[^""]+)""", "url"),
            ExtractRegexGroup(html, @"<img[^>]*src=""(?<url>[^""]+)""[^>]*class=""[^""]*hero-desktop[^""]*""", "url"),
            ExtractRegexGroup(html, @"<img[^>]*class=""[^""]*(?:program-hero|course-hero|featured-image)[^""]*""[^>]*src=""(?<url>[^""]+)""", "url"),
        };

        foreach (var candidate in candidates)
        {
            var absolute = NormalizeProgramLink(candidate, origin);
            if (!string.IsNullOrWhiteSpace(absolute) && !absolute.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                return absolute;
        }

        return null;
    }

    private static string? ExtractProgramDescription(string html)
    {
        var chunks = new List<string>();

        var overviewMatch = Regex.Match(
            html,
            @"<section[^>]*id=""overview""[^>]*>(?<body>.*?)</section>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (overviewMatch.Success)
            AppendParagraphText(overviewMatch.Groups["body"].Value, chunks, maxParagraphs: 8);

        if (chunks.Count == 0)
        {
            var introMatch = Regex.Match(
                html,
                @"<p[^>]*class=""[^""]*p-intro[^""]*""[^>]*>(?<text>.*?)</p>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (introMatch.Success)
            {
                var intro = StripHtml(introMatch.Groups["text"].Value);
                if (!string.IsNullOrWhiteSpace(intro))
                    chunks.Add(intro);
            }

            AppendParagraphText(html, chunks, maxParagraphs: 6);
        }

        if (chunks.Count == 0)
        {
            foreach (Match match in Regex.Matches(
                html,
                @"<script[^>]+type=""application/ld\+json""[^>]*>(?<json>.*?)</script>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                try
                {
                    using var document = JsonDocument.Parse(match.Groups["json"].Value);
                    var description = FindJsonDescription(document.RootElement);
                    if (!string.IsNullOrWhiteSpace(description))
                        chunks.Add(description);
                }
                catch (JsonException)
                {
                    // ignore invalid JSON-LD blocks
                }
            }
        }

        if (chunks.Count == 0)
        {
            var metaDescription = ExtractRegexGroup(html, @"name=""description""[^>]*content=""(?<text>[^""]+)""", "text")
                ?? ExtractRegexGroup(html, @"property=""og:description""[^>]*content=""(?<text>[^""]+)""", "text");
            if (!string.IsNullOrWhiteSpace(metaDescription))
                chunks.Add(WebUtility.HtmlDecode(metaDescription));
        }

        var combined = string.Join("\n\n", chunks.Distinct(StringComparer.OrdinalIgnoreCase)).Trim();
        if (combined.Length > 12000)
            combined = combined[..12000];

        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    private static void AppendParagraphText(string html, List<string> chunks, int maxParagraphs)
    {
        foreach (Match match in Regex.Matches(html, @"<p[^>]*>(?<text>.*?)</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
        {
            var text = StripHtml(match.Groups["text"].Value);
            if (text.Length < 40)
                continue;

            if (text.Contains("cookie", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("View the ", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("Download ", StringComparison.OrdinalIgnoreCase))
                continue;

            chunks.Add(text);
            if (chunks.Count >= maxParagraphs)
                break;
        }
    }

    private static string? FindJsonDescription(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("description", out var description)
                    && description.ValueKind == JsonValueKind.String)
                {
                    var value = description.GetString();
                    if (!string.IsNullOrWhiteSpace(value) && value.Length >= 40)
                        return value;
                }

                foreach (var property in element.EnumerateObject())
                {
                    var nested = FindJsonDescription(property.Value);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindJsonDescription(item);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
                break;
        }

        return null;
    }

    private static string? ExtractAdmissionRequirements(string html)
    {
        var sectionMatch = Regex.Match(
            html,
            @"<(?:section|div)[^>]*(?:id|class)=""[^""]*(?:admission|entry-require|eligibility|apply|enrol)[^""]*""[^>]*>(?<body>.*?)</(?:section|div)>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (!sectionMatch.Success)
            return null;

        var text = StripHtml(sectionMatch.Groups["body"].Value);
        text = Regex.Replace(text, @"\s+", " ").Trim();
        if (text.Length < 40)
            return null;

        return text.Length > 8000 ? text[..8000] : text;
    }

    private static string StripHtml(string html)
    {
        var withoutTags = Regex.Replace(html, "<[^>]+>", " ");
        return Regex.Replace(WebUtility.HtmlDecode(withoutTags), @"\s+", " ").Trim();
    }

    private static string? ExtractRegexGroup(string input, string pattern, string groupName)
    {
        var match = Regex.Match(input, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[groupName].Value.Trim()) : null;
    }

    private static string? NormalizeProgramLink(string? link, string? origin)
    {
        if (string.IsNullOrWhiteSpace(link))
            return null;

        var value = WebUtility.HtmlDecode(link.Trim());
        if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return value;

        if (origin is null)
            return value;

        return value.StartsWith('/')
            ? $"{origin}{value}"
            : $"{origin}/{value}";
    }

    private static string? NormalizeLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
            return null;

        var value = level.Trim();
        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static string? FirstNonEmptyString(string? first, string? second)
    {
        return !string.IsNullOrWhiteSpace(first) ? first : second;
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
