using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AvecADeskApi.DTOs.Trello;

namespace AvecADeskApi.Services.Trello;

public class TrelloApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public TrelloApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Thin wrapper over the Trello REST API. Key/token come from configuration
/// (user-secrets locally: Trello:ApiKey, Trello:Token) and are sent in the
/// Authorization header so they never appear in URLs or logs.
/// </summary>
public class TrelloClient
{
    public const string HttpClientName = "Trello";

    // Trello allows ~100 requests / 10 s per token; spacing writes keeps bulk syncs under it.
    private static readonly TimeSpan WriteSpacing = TimeSpan.FromMilliseconds(120);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public TrelloClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    private string? ApiKey => _configuration["Trello:ApiKey"];
    private string? Token => _configuration["Trello:Token"];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Token);

    public Task<TrelloMember> GetMeAsync(CancellationToken ct = default) =>
        SendAsync<TrelloMember>(HttpMethod.Get, "members/me?fields=id,fullName,username", null, ct);

    public Task<TrelloBoard> CreateBoardAsync(string name, CancellationToken ct = default) =>
        SendAsync<TrelloBoard>(HttpMethod.Post, "boards", new Dictionary<string, object?>
        {
            ["name"] = name,
            ["defaultLists"] = false,
        }, ct);

    public Task<TrelloBoard> RenameBoardAsync(string boardId, string name, CancellationToken ct = default) =>
        SendAsync<TrelloBoard>(HttpMethod.Put, $"boards/{Uri.EscapeDataString(boardId)}", new Dictionary<string, object?>
        {
            ["name"] = name,
        }, ct);

    /// <summary>
    /// Returns null when Trello says this callback/model/token webhook already exists.
    /// Trello calls HEAD on the callback URL first, so the API must be reachable from the internet.
    /// </summary>
    public async Task<TrelloWebhook?> CreateWebhookAsync(string callbackUrl, string idModel, string description, CancellationToken ct = default)
    {
        try
        {
            return await SendAsync<TrelloWebhook>(HttpMethod.Post, "webhooks", new Dictionary<string, object?>
            {
                ["callbackURL"] = callbackUrl,
                ["idModel"] = idModel,
                ["description"] = description,
            }, ct);
        }
        catch (TrelloApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest
                                            && ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
    }

    public async Task DeleteWebhookAsync(string webhookId, CancellationToken ct = default)
    {
        try
        {
            await SendAsync<JsonElement>(HttpMethod.Delete, $"webhooks/{Uri.EscapeDataString(webhookId)}", null, ct);
        }
        catch (TrelloApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
        }
        catch (JsonException)
        {
            // Trello may answer a delete with an empty body.
        }
    }

    public Task<List<TrelloBoard>> GetBoardsAsync(CancellationToken ct = default) =>
        SendAsync<List<TrelloBoard>>(HttpMethod.Get, "members/me/boards?filter=open&fields=name,url,closed", null, ct);

    public Task<List<TrelloList>> GetListsAsync(string boardId, CancellationToken ct = default) =>
        SendAsync<List<TrelloList>>(HttpMethod.Get, $"boards/{Uri.EscapeDataString(boardId)}/lists?filter=open&fields=name,closed,pos", null, ct);

    public Task<List<TrelloCard>> GetCardsAsync(string boardId, CancellationToken ct = default) =>
        SendAsync<List<TrelloCard>>(HttpMethod.Get, $"boards/{Uri.EscapeDataString(boardId)}/cards/open?fields=name,desc,due,start,idList,closed,pos,idLabels,cover", null, ct);

    public Task<TrelloList> CreateListAsync(string boardId, string name, CancellationToken ct = default) =>
        SendAsync<TrelloList>(HttpMethod.Post, "lists", new Dictionary<string, object?>
        {
            ["idBoard"] = boardId,
            ["name"] = name,
            ["pos"] = "bottom",
        }, ct);

    public Task<TrelloCard> CreateCardAsync(string listId, string name, string? desc, DateTime? dueUtc, CancellationToken ct = default) =>
        SendAsync<TrelloCard>(HttpMethod.Post, "cards", new Dictionary<string, object?>
        {
            ["idList"] = listId,
            ["name"] = name,
            ["desc"] = desc ?? string.Empty,
            ["due"] = dueUtc.HasValue ? DateTime.SpecifyKind(dueUtc.Value, DateTimeKind.Utc).ToString("o") : null,
            ["pos"] = "bottom",
        }, ct);

    /// <summary>Only the keys present in <paramref name="fields"/> are changed; a null value clears the field (e.g. due).</summary>
    public Task<TrelloCard> UpdateCardAsync(string cardId, Dictionary<string, object?> fields, CancellationToken ct = default) =>
        SendAsync<TrelloCard>(HttpMethod.Put, $"cards/{Uri.EscapeDataString(cardId)}", fields, ct);

    public Task<List<TrelloLabel>> GetBoardLabelsAsync(string boardId, CancellationToken ct = default) =>
        SendAsync<List<TrelloLabel>>(HttpMethod.Get, $"boards/{Uri.EscapeDataString(boardId)}/labels?fields=name,color&limit=1000", null, ct);

    /// <summary><paramref name="color"/> null = a label without color.</summary>
    public Task<TrelloLabel> CreateLabelAsync(string boardId, string name, string? color, CancellationToken ct = default) =>
        SendAsync<TrelloLabel>(HttpMethod.Post, "labels", new Dictionary<string, object?>
        {
            ["idBoard"] = boardId,
            ["name"] = name,
            ["color"] = color ?? "null",
        }, ct);

    public Task<TrelloLabel> UpdateLabelAsync(string labelId, string name, string? color, CancellationToken ct = default) =>
        SendAsync<TrelloLabel>(HttpMethod.Put, $"labels/{Uri.EscapeDataString(labelId)}", new Dictionary<string, object?>
        {
            ["name"] = name,
            ["color"] = color ?? "null",
        }, ct);

    public Task DeleteLabelAsync(string labelId, CancellationToken ct = default) =>
        DeleteAsync($"labels/{Uri.EscapeDataString(labelId)}", ct);

    public Task<List<TrelloChecklist>> GetBoardChecklistsAsync(string boardId, CancellationToken ct = default) =>
        SendAsync<List<TrelloChecklist>>(HttpMethod.Get,
            $"boards/{Uri.EscapeDataString(boardId)}/checklists?fields=name,idCard,pos&checkItems=all&checkItem_fields=name,state,pos", null, ct);

    public Task<TrelloChecklist> CreateChecklistAsync(string cardId, string name, CancellationToken ct = default) =>
        SendAsync<TrelloChecklist>(HttpMethod.Post, "checklists", new Dictionary<string, object?>
        {
            ["idCard"] = cardId,
            ["name"] = name,
            ["pos"] = "bottom",
        }, ct);

    public Task DeleteChecklistAsync(string checklistId, CancellationToken ct = default) =>
        DeleteAsync($"checklists/{Uri.EscapeDataString(checklistId)}", ct);

    public Task<TrelloCheckItem> CreateCheckItemAsync(string checklistId, string name, bool complete, CancellationToken ct = default) =>
        SendAsync<TrelloCheckItem>(HttpMethod.Post, $"checklists/{Uri.EscapeDataString(checklistId)}/checkItems", new Dictionary<string, object?>
        {
            ["name"] = name,
            ["pos"] = "bottom",
            ["checked"] = complete,
        }, ct);

    public Task<TrelloCheckItem> UpdateCheckItemAsync(string cardId, string checkItemId, string name, bool complete, CancellationToken ct = default) =>
        SendAsync<TrelloCheckItem>(HttpMethod.Put,
            $"cards/{Uri.EscapeDataString(cardId)}/checkItem/{Uri.EscapeDataString(checkItemId)}", new Dictionary<string, object?>
            {
                ["name"] = name,
                ["state"] = complete ? "complete" : "incomplete",
            }, ct);

    public Task DeleteCheckItemAsync(string checklistId, string checkItemId, CancellationToken ct = default) =>
        DeleteAsync($"checklists/{Uri.EscapeDataString(checklistId)}/checkItems/{Uri.EscapeDataString(checkItemId)}", ct);


    /// <summary>Newest first. <paramref name="before"/> is an action ID, used to page further back.</summary>
    public Task<List<TrelloCommentAction>> GetBoardCommentsAsync(string boardId, string? before, int limit, CancellationToken ct = default)
    {
        var path = $"boards/{Uri.EscapeDataString(boardId)}/actions?filter=commentCard&limit={limit}" +
                   "&fields=data,date,idMemberCreator&memberCreator_fields=fullName,username";
        if (!string.IsNullOrEmpty(before)) path += $"&before={Uri.EscapeDataString(before)}";
        return SendAsync<List<TrelloCommentAction>>(HttpMethod.Get, path, null, ct);
    }

    /// <summary>Newest first. <paramref name="since"/> = an action ID; only newer actions are returned.</summary>
    public Task<List<TrelloActivityAction>> GetBoardActivityAsync(string boardId, IEnumerable<string> types, string? since, string? before, int limit, CancellationToken ct = default)
    {
        var path = $"boards/{Uri.EscapeDataString(boardId)}/actions?filter={Uri.EscapeDataString(string.Join(',', types))}&limit={limit}" +
                   "&fields=type,data,date,idMemberCreator&memberCreator_fields=fullName,username&member=true&member_fields=fullName,username";
        if (!string.IsNullOrEmpty(since)) path += $"&since={Uri.EscapeDataString(since)}";
        if (!string.IsNullOrEmpty(before)) path += $"&before={Uri.EscapeDataString(before)}";
        return SendAsync<List<TrelloActivityAction>>(HttpMethod.Get, path, null, ct);
    }

    public Task<TrelloCommentAction> AddCommentAsync(string cardId, string text, CancellationToken ct = default) =>
        SendAsync<TrelloCommentAction>(HttpMethod.Post, $"cards/{Uri.EscapeDataString(cardId)}/actions/comments", new Dictionary<string, object?>
        {
            ["text"] = text,
        }, ct);

    /// <summary>Trello only lets the comment's author edit it.</summary>
    public Task<TrelloCommentAction> UpdateCommentAsync(string commentActionId, string text, CancellationToken ct = default) =>
        SendAsync<TrelloCommentAction>(HttpMethod.Put, $"actions/{Uri.EscapeDataString(commentActionId)}", new Dictionary<string, object?>
        {
            ["text"] = text,
        }, ct);

    public Task DeleteCommentAsync(string commentActionId, CancellationToken ct = default) =>
        DeleteAsync($"actions/{Uri.EscapeDataString(commentActionId)}", ct);

    private async Task DeleteAsync(string path, CancellationToken ct)
    {
        try
        {
            await SendAsync<JsonElement>(HttpMethod.Delete, path, null, ct);
        }
        catch (JsonException)
        {
            // Trello may answer a delete with an empty body.
        }
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Trello is not configured. Set Trello:ApiKey and Trello:Token.");

        var client = _httpClientFactory.CreateClient(HttpClientName);

        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "OAuth", $"oauth_consumer_key=\"{ApiKey}\", oauth_token=\"{Token}\"");
            if (body != null)
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < 4)
            {
                var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(10);
                await Task.Delay(wait, ct);
                continue;
            }

            var text = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                var detail = string.IsNullOrWhiteSpace(text) ? response.ReasonPhrase : text.Trim();
                if (detail?.Length > 300) detail = detail[..300];
                var message = response.StatusCode == HttpStatusCode.Unauthorized
                    ? "Trello rejected the API key/token (401). Check Trello:ApiKey and Trello:Token."
                    : $"Trello API error {(int)response.StatusCode}: {detail}";
                throw new TrelloApiException(response.StatusCode, message);
            }

            if (method != HttpMethod.Get)
                await Task.Delay(WriteSpacing, ct);

            return JsonSerializer.Deserialize<T>(text, JsonOptions)
                ?? throw new TrelloApiException(response.StatusCode, "Trello returned an empty response.");
        }
    }
}
