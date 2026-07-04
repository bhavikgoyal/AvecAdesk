using System.Text;
using System.Text.Json;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteScrapping;

namespace AvecADeskApi.Services;

public class InstituteScrappingService : IInstituteScrappingService
{
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

        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured. Add OpenAI:ApiKey in appsettings.json.");

        var (websiteText, scrapedLogoUrl, fetchErrors, usedBrowser, parsedPrograms) =
            await _websiteFetcher.FetchWebsiteTextAsync(websiteUrl);
        if (string.IsNullOrWhiteSpace(websiteText))
        {
            var detail = SummarizeFetchErrors(fetchErrors);
            throw new InvalidOperationException(
                $"Could not read live website content. {detail}");
        }

        var extraction = await ExtractFromLiveWebsiteTextAsync(instituteName, websiteUrl, websiteText);
        var institute = extraction.Institute;
        var extractedPrograms = MergePrograms(parsedPrograms, extraction.Programs);

        if (extractedPrograms.Count == 0)
            throw new InvalidOperationException("No program data could be extracted from the live website content. Try a programs listing URL.");

        var sharedLogo = FirstNonEmpty(scrapedLogoUrl, institute?.Logo);
        var sharedName = FirstNonEmpty(institute?.Name, instituteName);
        var sharedCountry = institute?.Country;
        var sharedCity = institute?.City;
        var sharedCountryRanking = institute?.CountryRanking;
        var sharedAbout = institute?.About;
        var sharedScholarships = institute?.ScholarshipsDetails;

        var upsertRequests = extractedPrograms.Select(program => new InstituteScrappingUpsertRequest
        {
            InstituteName = instituteName,
            WebsiteURL = websiteUrl,
            Campus = program.Campus,
            State = program.State,
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
            Description = sharedAbout,
            CountryRanking = sharedCountryRanking,
            ScholarshipsDetails = FirstNonEmpty(program.ScholarshipsDetails, sharedScholarships),
            ProgramDescription = program.ProgramDescription,
            ProgramLogo = program.ProgramLogo,
            AddmissionRequirements = program.AddmissionRequirements,
        }).ToList();

        var records = await _repository.CreateManyAsync(upsertRequests);

        return new InstituteScrappingRunResponse
        {
            RecordsInserted = records.Count,
            UsedAiFallback = false,
            Message = usedBrowser
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
                  "programDescription": "",
                  "programLogo": "",
                  "addmissionRequirements": "",
                  "scholarshipsDetails": ""
                }
              ]
            }
            Rules:
            - institute fields come from homepage/about pages: logo URL (full https URL if visible), country ranking, general about text, country, city, scholarships.
            - institute.logo must be a full image URL if found in the text (header logo, og:image, etc.).
            - institute.about must contain ALL About Us / institute overview text from the website — the complete description, not a summary.
            - programs[].programDescription must contain ALL program/course-specific description text (overview, what you will learn, course details, etc.) — the complete text, not a summary.
            - programs[].programLogo must be the full https URL of the program/course image or logo if found on the program page.
            - programs[].addmissionRequirements must contain ALL admission/entry requirements text for that program (eligibility, prerequisites, academic requirements, documents, etc.).
            - Do NOT put institute about text into programDescription or vice versa.
            - Extract ALL education offerings — degrees, diplomas, certificates, VET, qualifications, etc.
            - If a programs/courses listing page is present, include EVERY listed program in programs[] — do not stop at 2 or 10; include all visible programs.
            - Extract ONLY from the website text. NEVER invent data.
            - Use empty string when a field is not present.
            - programLink must be a full URL only if it appears in the text.
            """;

        var userPrompt = $"""
            Institute name: {instituteName}
            Website URL: {websiteUrl}

            Live website text scraped from the site:
            {websiteText}
            """;

        return await CallChatGptForExtractionAsync(model, apiKey, systemPrompt, userPrompt);
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

    private static List<ChatGptProgramRecord> MergePrograms(
        IReadOnlyList<ScrapedProgramRecord> htmlPrograms,
        IReadOnlyList<ChatGptProgramRecord> gptPrograms)
    {
        if (htmlPrograms.Count == 0)
            return gptPrograms.ToList();

        var gptByLink = gptPrograms
            .Where(program => !string.IsNullOrWhiteSpace(program.ProgramLink))
            .GroupBy(program => NormalizeProgramKey(program.ProgramLink!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var gptByName = gptPrograms
            .Where(program => !string.IsNullOrWhiteSpace(program.ProgramName))
            .GroupBy(program => NormalizeProgramKey(program.ProgramName!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var merged = new List<ChatGptProgramRecord>();
        var consumedGpt = new HashSet<ChatGptProgramRecord>();

        foreach (var htmlProgram in htmlPrograms)
        {
            ChatGptProgramRecord? gptProgram = null;

            if (!string.IsNullOrWhiteSpace(htmlProgram.ProgramLink)
                && gptByLink.TryGetValue(NormalizeProgramKey(htmlProgram.ProgramLink), out var byLink))
            {
                gptProgram = byLink;
            }
            else if (!string.IsNullOrWhiteSpace(htmlProgram.ProgramName)
                && gptByName.TryGetValue(NormalizeProgramKey(htmlProgram.ProgramName), out var byName))
            {
                gptProgram = byName;
            }

            if (gptProgram is not null)
                consumedGpt.Add(gptProgram);

            merged.Add(new ChatGptProgramRecord
            {
                ProgramName = htmlProgram.ProgramName,
                Level = FirstNonEmpty(htmlProgram.Level, gptProgram?.Level),
                ProgramLink = FirstNonEmpty(htmlProgram.ProgramLink, gptProgram?.ProgramLink),
                Campus = FirstNonEmpty(htmlProgram.Campus, gptProgram?.Campus),
                State = FirstNonEmpty(htmlProgram.State, gptProgram?.State),
                CricosCode = FirstNonEmpty(htmlProgram.CricosCode, gptProgram?.CricosCode),
                Duration = FirstNonEmpty(htmlProgram.Duration, gptProgram?.Duration),
                Intake = FirstNonEmpty(htmlProgram.Intake, gptProgram?.Intake),
                FeesYearly = FirstNonEmpty(htmlProgram.FeesYearly, gptProgram?.FeesYearly),
                EnglishReq = FirstNonEmpty(htmlProgram.EnglishReq, gptProgram?.EnglishReq),
                ProgramDescription = FirstNonEmpty(htmlProgram.ProgramDescription, gptProgram?.ProgramDescription),
                ProgramLogo = FirstNonEmpty(htmlProgram.ProgramLogo, gptProgram?.ProgramLogo),
                AddmissionRequirements = FirstNonEmpty(htmlProgram.AddmissionRequirements, gptProgram?.AddmissionRequirements),
                ScholarshipsDetails = FirstNonEmpty(gptProgram?.ScholarshipsDetails, htmlProgram.ScholarshipsDetails),
            });
        }

        foreach (var gptProgram in gptPrograms)
        {
            if (consumedGpt.Contains(gptProgram) || string.IsNullOrWhiteSpace(gptProgram.ProgramName))
                continue;

            merged.Add(gptProgram);
        }

        return merged;
    }

    private static string NormalizeProgramKey(string value)
    {
        return value.Trim().TrimEnd('/').ToLowerInvariant();
    }

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
            return "The website blocked automated access (403 Forbidden). " +
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
        public string? ProgramDescription { get; set; }
        public string? ProgramLogo { get; set; }
        public string? AddmissionRequirements { get; set; }
        public string? ScholarshipsDetails { get; set; }
    }
}
