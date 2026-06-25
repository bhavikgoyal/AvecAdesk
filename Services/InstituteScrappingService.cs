using System.Text;
using System.Text.Json;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteScrapping;

namespace AvecADeskApi.Services;

public class InstituteScrappingService : IInstituteScrappingService
{
    private const int GptBatchCharLimit = 60_000;

    private readonly IInstituteScrappingRepository _repository;
    private readonly IInstituteWebsiteFetcher _websiteFetcher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly LogHelper _logHelper;

    public InstituteScrappingService(
        IInstituteScrappingRepository repository,
        IInstituteWebsiteFetcher websiteFetcher,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        LogHelper logHelper)
    {
        _repository = repository;
        _websiteFetcher = websiteFetcher;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logHelper = logHelper;
    }

    public async Task<InstituteScrappingRunResponse> RunScrapeAsync(InstituteScrappingRunRequest request)
    {
        var instituteName = request.InstituteName?.Trim();
        var websiteUrl = NormalizeUrl(request.WebsiteURL);

        if (string.IsNullOrWhiteSpace(instituteName))
            throw new InvalidOperationException("Institute name is required.");

        if (string.IsNullOrWhiteSpace(websiteUrl))
            throw new InvalidOperationException("Website URL is required.");

        var maxTotalSeconds = _configuration.GetValue("Scraping:MaxTotalSeconds", 0);
        using var timeoutCts = maxTotalSeconds > 0
            ? new CancellationTokenSource(TimeSpan.FromSeconds(maxTotalSeconds))
            : null;
        var scrapeToken = timeoutCts?.Token ?? CancellationToken.None;

        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured. Add OpenAI:ApiKey in appsettings.json.");

        var (websiteText, scrapedLogoUrl, fetchErrors, usedBrowser) =
            await _websiteFetcher.FetchWebsiteTextAsync(websiteUrl, scrapeToken);
        if (string.IsNullOrWhiteSpace(websiteText))
        {
            var detail = SummarizeFetchErrors(fetchErrors);
            throw new InvalidOperationException(
                $"Could not read live website content. {detail}");
        }

        var extraction = await ExtractFromLiveWebsiteTextAsync(instituteName, websiteUrl, websiteText);
        var institute = extraction.Institute;
        var extractedPrograms = extraction.Programs;

        if (extractedPrograms.Count == 0)
            throw new InvalidOperationException("No program data could be extracted from the live website content. Try a programs listing URL.");

        var sharedLogo = FirstNonEmpty(scrapedLogoUrl, institute?.Logo);
        var sharedName = FirstNonEmpty(institute?.Name, instituteName);
        var sharedCountry = institute?.Country;
        var sharedCity = institute?.City;
        var sharedCountryRanking = institute?.CountryRanking;
        var sharedScholarships = institute?.ScholarshipsDetails;

        var upsertRequests = extractedPrograms.Select(program =>
        {
            var campus = ResolveCampus(program, sharedCity, sharedCountry, websiteUrl);
            var state = ResolveState(program, campus, sharedCountry);

            return new InstituteScrappingUpsertRequest
            {
                InstituteName = instituteName,
                WebsiteURL = websiteUrl,
                Campus = campus,
                State = state,
                ProgramName = program.ProgramName,
                Level = program.Level,
                ProgramLink = program.ProgramLink,
                CricosCode = program.CricosCode,
                Duration = program.Duration,
                Intake = program.Intake,
                FeesYearly = program.FeesYearly,
                EnglishReq = program.EnglishReq,
                Name = sharedName,
                Logo = sharedLogo,
                Country = sharedCountry,
                City = sharedCity,
                Description = program.Description,
                CountryRanking = sharedCountryRanking,
                ScholarshipsDetails = FirstNonEmpty(program.ScholarshipsDetails, sharedScholarships),
            };
        }).ToList();

        var records = await _repository.CreateManyAsync(upsertRequests);

        return new InstituteScrappingRunResponse
        {
            RecordsInserted = records.Count,
            UsedAiFallback = false,
            Message = fetchErrors.Count > 0
                ? $"Saved {records.Count} program(s). Some pages were skipped ({fetchErrors.Count} errors)."
                : usedBrowser
                    ? $"Programs extracted from live website using browser fetch and saved successfully ({records.Count} rows)."
                    : $"Programs extracted from live website content and saved successfully ({records.Count} rows).",
            Records = records,
        };
    }

    private async Task<ChatGptExtractionResult> ExtractFromLiveWebsiteTextAsync(
        string instituteName,
        string websiteUrl,
        string websiteText)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var apiKey = _configuration["OpenAI:ApiKey"]!;

        var systemPrompt = """
            You extract structured education data ONLY from the provided live website text.
            Return ONLY valid JSON with this shape:
            {
              "institute": {
                "name": "",
                "logo": "",
                "country": "",
                "city": "",
                "about": "",
                "countryRanking": "",
                "scholarshipsDetails": ""
              },
              "programs": [
                {
                  "campus": "",
                  "state": "",
                  "programName": "",
                  "level": "",
                  "programLink": "",
                  "cricosCode": "",
                  "duration": "",
                  "intake": "",
                  "feesYearly": "",
                  "englishReq": "",
                  "description": "",
                  "scholarshipsDetails": ""
                }
              ]
            }
            Rules:
            - institute fields come from homepage/about pages: logo URL (full https URL if visible), country ranking, general about text, country, city, scholarships.
            - institute.logo must be a full image URL if found in the text (header logo, og:image, etc.).
            - institute.about is the general institute description — NOT program-specific text.
            - programs[].campus is IMPORTANT: the physical campus or city where the program is delivered.
              * Australian universities: use official campus names exactly as on the site (e.g. PERTH, SYDNEY, FREMANTLE, BROOME).
              * US/international universities: use the campus name, or the institute city (e.g. "Notre Dame"), or "Main Campus" when only one location exists.
              * If a program page mentions a city or campus but not the word "campus", still fill programs[].campus from that location.
              * NEVER leave campus empty when institute.city or any program location is known — use institute.city as the default campus for all programs.
            - programs[].state: Australian state code (WA, NSW, VIC, QLD, etc.) or US state code (IN, CA, NY, etc.) when the location is known.
            - programs[].description MUST be the unique program-specific overview/description from that program's own page section (look for text under the program title, "Why study", overview, about this course, etc.).
            - NEVER copy the same institute.about text into every program description. Leave description empty if no program-specific text exists for that program.
            - When a page marker like "--- Page: https://... ---" appears, use content from THAT page for the matching program (match by programLink URL or program name on that page).
            - Extract ALL education offerings visible in the text — degrees, diplomas, certificates, VET, qualifications, etc. Do not stop at 20.
            - Extract ONLY from the website text. NEVER invent data.
            - Use empty string when a field is not present.
            - programLink must be a full URL only if it appears in the text.
            """;

        var batches = SplitWebsiteTextIntoBatches(websiteText, GptBatchCharLimit);
        ChatGptInstituteRecord? mergedInstitute = null;
        var mergedPrograms = new List<ChatGptProgramRecord>();

        for (var i = 0; i < batches.Count; i++)
        {
            var batchNote = batches.Count > 1
                ? $"\nNote: This is batch {i + 1} of {batches.Count}. Extract all programs in this batch."
                : string.Empty;

            var userPrompt = $"""
                Institute name: {instituteName}
                Website URL: {websiteUrl}{batchNote}

                Live website text scraped from the site:
                {batches[i]}
                """;

            var batchResult = await CallChatGptForExtractionAsync(model, apiKey, systemPrompt, userPrompt);

            if (mergedInstitute is null && batchResult.Institute is not null)
                mergedInstitute = batchResult.Institute;

            foreach (var program in batchResult.Programs)
                MergeProgram(mergedPrograms, program);
        }

        return new ChatGptExtractionResult
        {
            Institute = mergedInstitute,
            Programs = mergedPrograms,
        };
    }

    private static List<string> SplitWebsiteTextIntoBatches(string websiteText, int maxChars)
    {
        if (websiteText.Length <= maxChars)
            return new List<string> { websiteText };

        var pageMarker = "--- Page:";
        var sections = websiteText.Split(pageMarker, StringSplitOptions.RemoveEmptyEntries);
        if (sections.Length <= 1)
        {
            return ChunkByLength(websiteText, maxChars);
        }

        var batches = new List<string>();
        var current = new StringBuilder();

        foreach (var section in sections)
        {
            var piece = sections[0] == section && !websiteText.StartsWith(pageMarker, StringComparison.Ordinal)
                ? section
                : pageMarker + section;

            if (current.Length > 0 && current.Length + piece.Length > maxChars)
            {
                batches.Add(current.ToString());
                current.Clear();
            }

            current.Append(piece);
        }

        if (current.Length > 0)
            batches.Add(current.ToString());

        return batches.Count > 0 ? batches : ChunkByLength(websiteText, maxChars);
    }

    private static List<string> ChunkByLength(string text, int maxChars)
    {
        var chunks = new List<string>();
        for (var offset = 0; offset < text.Length; offset += maxChars)
        {
            var length = Math.Min(maxChars, text.Length - offset);
            chunks.Add(text.Substring(offset, length));
        }

        return chunks;
    }

    private static void MergeProgram(List<ChatGptProgramRecord> merged, ChatGptProgramRecord incoming)
    {
        if (string.IsNullOrWhiteSpace(incoming.ProgramName))
            return;

        var key = ProgramKey(incoming);
        var existing = merged.FirstOrDefault(p => ProgramKey(p) == key);

        if (existing is null)
        {
            merged.Add(incoming);
            return;
        }

        existing.Campus = FirstNonEmpty(existing.Campus, incoming.Campus);
        existing.State = FirstNonEmpty(existing.State, incoming.State);
        existing.ProgramName = FirstNonEmpty(existing.ProgramName, incoming.ProgramName);
        existing.Level = FirstNonEmpty(existing.Level, incoming.Level);
        existing.ProgramLink = FirstNonEmpty(existing.ProgramLink, incoming.ProgramLink);
        existing.CricosCode = FirstNonEmpty(existing.CricosCode, incoming.CricosCode);
        existing.Duration = FirstNonEmpty(existing.Duration, incoming.Duration);
        existing.Intake = FirstNonEmpty(existing.Intake, incoming.Intake);
        existing.FeesYearly = FirstNonEmpty(existing.FeesYearly, incoming.FeesYearly);
        existing.EnglishReq = FirstNonEmpty(existing.EnglishReq, incoming.EnglishReq);
        existing.Description = FirstNonEmpty(incoming.Description, existing.Description);
        existing.ScholarshipsDetails = FirstNonEmpty(existing.ScholarshipsDetails, incoming.ScholarshipsDetails);
    }

    private static string ProgramKey(ChatGptProgramRecord program)
    {
        if (!string.IsNullOrWhiteSpace(program.ProgramLink))
            return program.ProgramLink.Trim().ToLowerInvariant();

        return program.ProgramName?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private async Task<ChatGptExtractionResult> CallChatGptForExtractionAsync(
        string model,
        string apiKey,
        string systemPrompt,
        string userPrompt)
    {
        var payload = new
        {
            model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };

        var client = _httpClientFactory.CreateClient("OpenAI");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var httpResponse = await client.SendAsync(httpRequest);
        var responseBody = await httpResponse.Content.ReadAsStringAsync();

        if (!httpResponse.IsSuccessStatusCode)
        {
            var apiMessage = TryReadOpenAiError(responseBody);
            _logHelper.LogError(nameof(CallChatGptForExtractionAsync), new Exception(responseBody));

            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException(
                    "OpenAI API key is invalid or expired. Update OpenAI:ApiKey in appsettings.json and restart the API.");
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(apiMessage)
                    ? "ChatGPT request failed. Check your OpenAI API key, billing credits, and model configuration."
                    : $"ChatGPT request failed: {apiMessage}");
        }

        using var document = JsonDocument.Parse(responseBody);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            return new ChatGptExtractionResult();

        var parsed = JsonSerializer.Deserialize<ChatGptExtractionResult>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        parsed ??= new ChatGptExtractionResult();
        parsed.Programs = parsed.Programs?
            .Where(program => !string.IsNullOrWhiteSpace(program.ProgramName))
            .ToList() ?? new List<ChatGptProgramRecord>();

        return parsed;
    }

    private static string? ResolveCampus(
        ChatGptProgramRecord program,
        string? instituteCity,
        string? instituteCountry,
        string websiteUrl)
    {
        if (!string.IsNullOrWhiteSpace(program.Campus))
            return program.Campus.Trim();

        var context = $"{program.ProgramName} {program.ProgramLink} {program.Description}".ToUpperInvariant();

        foreach (var campus in AustralianCampusNames)
        {
            if (context.Contains(campus, StringComparison.Ordinal))
                return ToTitleCaseCampus(campus);
        }

        if (!string.IsNullOrWhiteSpace(instituteCity))
            return instituteCity.Trim();

        if (Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri)
            && uri.Host.Contains("notredame.edu.au", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return null;
    }

    private static string? ResolveState(
        ChatGptProgramRecord program,
        string? campus,
        string? instituteCountry)
    {
        if (!string.IsNullOrWhiteSpace(program.State))
            return program.State.Trim().ToUpperInvariant();

        var campusKey = campus?.Trim().ToUpperInvariant() ?? string.Empty;
        if (AustralianCampusToState.TryGetValue(campusKey, out var auState))
            return auState;

        var country = instituteCountry?.Trim().ToUpperInvariant() ?? string.Empty;
        if (country is "USA" or "US" or "UNITED STATES")
        {
            if (campusKey is "NOTRE DAME")
                return "IN";
        }

        return null;
    }

    private static string ToTitleCaseCampus(string campus) =>
        campus.Length switch
        {
            <= 3 => campus.ToUpperInvariant(),
            _ => char.ToUpperInvariant(campus[0]) + campus[1..].ToLowerInvariant(),
        };

    private static readonly string[] AustralianCampusNames =
    {
        "PERTH", "SYDNEY", "FREMANTLE", "BROOME", "MELBOURNE", "BRISBANE",
        "ADELAIDE", "DARWIN", "HOBART", "CANBERRA", "GOLD COAST",
    };

    private static readonly Dictionary<string, string> AustralianCampusToState =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["PERTH"] = "WA",
            ["FREMANTLE"] = "WA",
            ["BROOME"] = "WA",
            ["SYDNEY"] = "NSW",
            ["MELBOURNE"] = "VIC",
            ["BRISBANE"] = "QLD",
            ["GOLD COAST"] = "QLD",
            ["ADELAIDE"] = "SA",
            ["DARWIN"] = "NT",
            ["HOBART"] = "TAS",
            ["CANBERRA"] = "ACT",
        };

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static string SummarizeFetchErrors(IReadOnlyList<string> fetchErrors)
    {
        if (fetchErrors.Count == 0)
            return "No readable content returned from the website.";

        var unique = fetchErrors
            .Select(error => error.Contains("(browser):", StringComparison.OrdinalIgnoreCase)
                ? error[(error.IndexOf("(browser):", StringComparison.OrdinalIgnoreCase) + "(browser):".Length)..].Trim()
                : error)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        if (unique.Any(e => e.Contains("Browser not installed", StringComparison.OrdinalIgnoreCase)
            || e.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)))
        {
            return "Scraping browser is not installed on the server. " +
                   "Run: powershell -ExecutionPolicy Bypass -File bin\\Debug\\net10.0\\playwright.ps1 install chromium " +
                   "then restart the API.";
        }

        if (unique.Any(e => e.Contains("403", StringComparison.OrdinalIgnoreCase)))
        {
            return "The website blocked some pages (403 Forbidden). " +
                   "Install Playwright Chromium for browser-based scraping, or try the institute programs listing URL directly. " +
                   $"Details: {string.Join(" | ", unique)}";
        }

        return string.Join(" | ", unique);
    }

    private static string NormalizeUrl(string? url)
    {
        var value = url?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            value = $"https://{value}";
        }

        return value;
    }

    private static string? TryReadOpenAiError(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch
        {
            // ignore parse errors
        }

        return null;
    }

    private sealed class ChatGptExtractionResult
    {
        public ChatGptInstituteRecord? Institute { get; set; }
        public List<ChatGptProgramRecord> Programs { get; set; } = new();
    }

    private sealed class ChatGptInstituteRecord
    {
        public string? Name { get; set; }
        public string? Logo { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? About { get; set; }
        public string? CountryRanking { get; set; }
        public string? ScholarshipsDetails { get; set; }
    }

    private sealed class ChatGptProgramRecord
    {
        public string? Campus { get; set; }
        public string? State { get; set; }
        public string? ProgramName { get; set; }
        public string? Level { get; set; }
        public string? ProgramLink { get; set; }
        public string? CricosCode { get; set; }
        public string? Duration { get; set; }
        public string? Intake { get; set; }
        public string? FeesYearly { get; set; }
        public string? EnglishReq { get; set; }
        public string? Description { get; set; }
        public string? ScholarshipsDetails { get; set; }
    }
}
