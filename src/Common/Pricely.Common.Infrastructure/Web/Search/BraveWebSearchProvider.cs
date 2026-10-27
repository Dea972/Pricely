using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Pricely.Common.Application.Web;

namespace Pricely.Common.Infrastructure.Web.Search;

/// <summary>
/// Brave Search API (<c>GET res/v1/web/search</c>): one billed search per request, no LLM tokens.
/// </summary>
internal sealed partial class BraveWebSearchProvider(HttpClient httpClient, IOptionsMonitor<WebSearchOptions> options) : IWebSearchProvider
{
    public string Name => WebSearchProviders.Brave;

    public async Task<WebSearchResults> SearchAsync(WebSearchRequest request, string sessionId, CancellationToken cancellationToken)
    {
        string apiKey = options.CurrentValue.Brave.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(WebSearchErrors.NotConfigured(Name).Description);
        }

        string query = string.Create(
            CultureInfo.InvariantCulture,
            $"res/v1/web/search?q={Uri.EscapeDataString(request.Query)}&count={request.MaxResults}&country={Uri.EscapeDataString(request.Country)}&search_lang=fr");

        using var message = new HttpRequestMessage(HttpMethod.Get, new Uri(query, UriKind.Relative));
        message.Headers.Add("Accept", "application/json");
        message.Headers.Add("X-Subscription-Token", apiKey);

        using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Brave answered {(int)response.StatusCode}", inner: null, statusCode: response.StatusCode);
        }

        await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);

        var results = new List<WebSearchResult>();
        if (document.RootElement.TryGetProperty("web", out JsonElement web) && web.TryGetProperty("results", out JsonElement items))
        {
            foreach (JsonElement item in items.EnumerateArray())
            {
                if (Text(item, "url") is string url
                    && Uri.TryCreate(url, UriKind.Absolute, out Uri? address)
                    && results.TrueForAll(r => r.Address != address))
                {
                    results.Add(new WebSearchResult(
                        Text(item, "title") ?? address.Host,
                        address,
                        Text(item, "description"),
                        Text(item, "age") ?? Text(item, "page_age")));
                }
            }
        }

        return new WebSearchResults(Name, results, Searches: 1);
    }

    /// <summary>A text field, without the highlighting markup Brave puts in titles and descriptions.</summary>
    private static string? Text(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out JsonElement value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        string text = WebUtility.HtmlDecode(Markup().Replace(value.GetString() ?? string.Empty, string.Empty)).Trim();

        return text.Length == 0 ? null : text;
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.None, 1000)]
    private static partial Regex Markup();
}
