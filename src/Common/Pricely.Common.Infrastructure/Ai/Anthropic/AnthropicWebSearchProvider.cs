using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Web;
using Pricely.Common.Infrastructure.Web.Search;

namespace Pricely.Common.Infrastructure.Ai.Anthropic;

/// <summary>
/// Web search through Claude's server tool (<c>web_search_20260318</c>, called directly: no dynamic filtering, so the
/// result blocks come back as is with any model). The model of <see cref="WebSearchFeatures.Search"/> runs the searches
/// and quotes what the pages show; the result list comes from the <c>web_search_tool_result</c> blocks, the snippets from
/// the citations (<c>cited_text</c>). Billed per search (<c>usage.server_tool_use.web_search_requests</c>) plus the tokens,
/// journaled as an LLM call of the feature.
/// </summary>
internal sealed class AnthropicWebSearchProvider(
    HttpClient httpClient,
    IOptions<AnthropicOptions> anthropicOptions,
    IOptionsMonitor<WebSearchOptions> searchOptions,
    ILlmTargetResolver targetResolver,
    IEventBus eventBus,
    IDateTimeProvider dateTimeProvider)
    : IWebSearchProvider
{
    private const int MaxContinuations = 3;
    private const int MaxTokens = 4096;

    private const string Instructions = """
        Tu es le moteur de recherche web de Pricely, un comparateur de prix. Cherche sur le web ce que demande
        l'utilisateur (une ou deux recherches suffisent), puis cite chaque page utile en une phrase qui reprend ce
        qu'elle affiche : produit, conditionnement, prix, enseigne. Ne réponds pas à la question, n'invente rien,
        ne conclus pas : Pricely vérifiera lui-même les pages.
        """;

    public string Name => WebSearchProviders.Anthropic;

    public async Task<WebSearchResults> SearchAsync(WebSearchRequest request, string sessionId, CancellationToken cancellationToken)
    {
        LlmTarget? target = await targetResolver.ResolveAsync(WebSearchFeatures.Search, cancellationToken);
        string model = target is { Provider: AiProviderKeys.Anthropic, Model: string configured } ? configured : anthropicOptions.Value.DefaultModel;

        var messages = new JsonArray { new JsonObject { ["role"] = "user", ["content"] = request.Query } };
        var collected = new SearchCollector();
        var usage = new Usage();
        DateTime calledOnUtc = dateTimeProvider.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        string? errorMessage = null;

        try
        {
            for (int turn = 0; turn <= MaxContinuations; turn++)
            {
                using JsonDocument response = await SendAsync(Body(model, messages, request), cancellationToken);
                JsonElement root = response.RootElement;
                usage.Add(root);

                JsonElement content = root.GetProperty("content");
                collected.Read(content);

                // A long search turn is paused: the paused assistant message goes back unchanged to resume it.
                if (!string.Equals(root.GetProperty("stop_reason").GetString(), "pause_turn", StringComparison.Ordinal))
                {
                    break;
                }

                messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = JsonNode.Parse(content.GetRawText()) });
            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            errorMessage = exception.Message;

            // A turn that fails after another one searched: those searches are billed.
            throw new WebSearchFailedException(exception.Message, usage.Searches, exception);
        }
        finally
        {
            stopwatch.Stop();
            await PublishCallAsync(sessionId, request.UserId, model, usage, calledOnUtc, stopwatch.ElapsedMilliseconds, errorMessage, cancellationToken);
        }

        if (collected.Results.Count == 0 && collected.Errors.Count > 0)
        {
            throw new WebSearchFailedException($"web_search: {string.Join(", ", collected.Errors)}", usage.Searches);
        }

        return new WebSearchResults(Name, [.. collected.Results.Take(request.MaxResults)], usage.Searches);
    }

    private JsonObject Body(string model, JsonArray messages, WebSearchRequest request) =>
        new()
        {
            ["model"] = model,
            ["max_tokens"] = MaxTokens,
            ["system"] = Instructions,
            ["messages"] = JsonNode.Parse(messages.ToJsonString()),
            ["tools"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "web_search_20260318",
                    ["name"] = "web_search",
                    ["max_uses"] = Math.Max(1, searchOptions.CurrentValue.MaxSearchesPerRequest),
                    ["allowed_callers"] = new JsonArray { "direct" },
                    ["user_location"] = new JsonObject { ["type"] = "approximate", ["country"] = request.Country }
                }
            }
        };

    private async Task<JsonDocument> SendAsync(JsonObject body, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(new Uri("v1/messages", UriKind.Relative), body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException($"Anthropic answered {(int)response.StatusCode}: {error}", inner: null, statusCode: response.StatusCode);
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private Task PublishCallAsync(
        string sessionId,
        string? userId,
        string model,
        Usage usage,
        DateTime calledOnUtc,
        long responseTimeMs,
        string? errorMessage,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(
            new LlmCalledIntegrationEvent(Guid.CreateVersion7(), calledOnUtc)
            {
                SessionId = sessionId,
                FeatureCode = WebSearchFeatures.Search.Code,
                SourceModule = WebSearchFeatures.Search.SourceModule,
                UserId = userId,
                AgentName = AgentToolCall.Current?.AgentName,
                ModelProvider = AiProviderKeys.Anthropic,
                ModelName = usage.Model ?? model,
                InputTokens = usage.InputTokens,
                InputCachedTokens = usage.CacheReadTokens,
                InputCacheCreationTokens = usage.CacheCreationTokens,
                OutputTokens = usage.OutputTokens,
                ResponseTimeMs = responseTimeMs,
                IsSuccess = errorMessage is null,
                ErrorMessage = errorMessage
            },
            cancellationToken);

    /// <summary>The result pages in order, without duplicates, with the first text the model cited from each.</summary>
    internal sealed class SearchCollector
    {
        private readonly List<(string Title, Uri Address, string? PageAge)> _pages = [];
        private readonly Dictionary<string, string> _snippets = new(StringComparer.Ordinal);

        public List<string> Errors { get; } = [];

        public IReadOnlyList<WebSearchResult> Results =>
            [.. _pages.Select(p => new WebSearchResult(p.Title, p.Address, _snippets.GetValueOrDefault(p.Address.AbsoluteUri), p.PageAge))];

        public void Read(JsonElement content)
        {
            foreach (JsonElement block in content.EnumerateArray())
            {
                switch (Text(block, "type"))
                {
                    case "web_search_tool_result":
                        ReadResults(block.GetProperty("content"));
                        break;
                    case "text" when block.TryGetProperty("citations", out JsonElement citations) && citations.ValueKind == JsonValueKind.Array:
                        ReadCitations(citations);
                        break;
                }
            }
        }

        private void ReadResults(JsonElement results)
        {
            // A failed search is an object with an error code, a successful one a list (possibly empty).
            if (results.ValueKind != JsonValueKind.Array)
            {
                Errors.Add(Text(results, "error_code") ?? "unknown");

                return;
            }

            foreach (JsonElement result in results.EnumerateArray())
            {
                if (Text(result, "url") is string url
                    && Uri.TryCreate(url, UriKind.Absolute, out Uri? address)
                    && _pages.TrueForAll(p => p.Address != address))
                {
                    _pages.Add((Text(result, "title") ?? address.Host, address, Text(result, "page_age")));
                }
            }
        }

        private void ReadCitations(JsonElement citations)
        {
            foreach (JsonElement citation in citations.EnumerateArray())
            {
                if (Text(citation, "url") is string url
                    && Uri.TryCreate(url, UriKind.Absolute, out Uri? address)
                    && Text(citation, "cited_text") is string cited)
                {
                    _snippets.TryAdd(address.AbsoluteUri, cited);
                }
            }
        }

        private static string? Text(JsonElement element, string property) =>
            element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private sealed class Usage
    {
        public string? Model { get; private set; }

        public int InputTokens { get; private set; }

        public int OutputTokens { get; private set; }

        public int CacheReadTokens { get; private set; }

        public int CacheCreationTokens { get; private set; }

        public int Searches { get; private set; }

        public void Add(JsonElement response)
        {
            Model ??= response.TryGetProperty("model", out JsonElement model) ? model.GetString() : null;
            if (!response.TryGetProperty("usage", out JsonElement usage))
            {
                return;
            }

            InputTokens += Number(usage, "input_tokens");
            OutputTokens += Number(usage, "output_tokens");
            CacheReadTokens += Number(usage, "cache_read_input_tokens");
            CacheCreationTokens += Number(usage, "cache_creation_input_tokens");
            if (usage.TryGetProperty("server_tool_use", out JsonElement serverTools))
            {
                Searches += Number(serverTools, "web_search_requests");
            }
        }

        private static int Number(JsonElement element, string property) =>
            element.TryGetProperty(property, out JsonElement value) && value.TryGetInt32(out int number) ? number : 0;
    }
}
