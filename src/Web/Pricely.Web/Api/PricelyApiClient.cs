using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Pricely.Web.Authentication;

namespace Pricely.Web.Api;

/// <summary>
/// Typed client of the Pricely API (routes under <c>/api</c>), with the access token of the current user. Each call
/// either returns the response or throws <see cref="PricelyApiException"/> / <see cref="SessionExpiredException"/>.
/// </summary>
internal sealed class PricelyApiClient(HttpClient http, AccessTokenProvider tokens)
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<ProductDto>> SearchProductsAsync(string text, int limit, CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<ProductDto>>($"catalog/products?text={Uri.EscapeDataString(text)}&limit={Number(limit)}", cancellationToken);

    public Task<ProductDto> GetProductAsync(Guid productId, CancellationToken cancellationToken) =>
        GetAsync<ProductDto>($"catalog/products/{productId}", cancellationToken);

    public Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<ProductCategoryDto>>("catalog/categories", cancellationToken);

    public Task<IReadOnlyList<MerchantDto>> GetMerchantsAsync(CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<MerchantDto>>("pricing/merchants", cancellationToken);

    public Task<ProductOffersDto> GetProductOffersAsync(Guid productId, CancellationToken cancellationToken) =>
        GetAsync<ProductOffersDto>($"pricing/products/{productId}/offers", cancellationToken);

    public Task<IReadOnlyList<MerchantPriceDto>> GetPriceComparisonAsync(Guid productId, CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<MerchantPriceDto>>($"pricing/products/{productId}/prices", cancellationToken);

    public Task<IReadOnlyList<PriceHistoryEntryDto>> GetPriceHistoryAsync(Guid productId, DateTime fromUtc, CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<PriceHistoryEntryDto>>(
            $"pricing/products/{productId}/price-history?fromUtc={Uri.EscapeDataString(fromUtc.ToString("O", CultureInfo.InvariantCulture))}&limit=500",
            cancellationToken);

    public Task<CollectionPlanDto> GetCollectionPlanAsync(Guid productId, CancellationToken cancellationToken) =>
        GetAsync<CollectionPlanDto>($"pricing/products/{productId}/collection-plan", cancellationToken);

    public Task<IReadOnlyList<PendingOfferDto>> GetOffersPendingReviewAsync(CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<PendingOfferDto>>("pricing/offers/pending-review", cancellationToken);

    public Task<CollectResultDto> CollectProductOffersAsync(Guid productId, CancellationToken cancellationToken) =>
        SendAsync<CollectResultDto>(HttpMethod.Post, $"pricing/products/{productId}/collect", cancellationToken);

    public Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(string agentName, CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<ConversationSummaryDto>>($"agents/conversations?agentName={Uri.EscapeDataString(agentName)}", cancellationToken);

    public Task<ConversationDto> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken) =>
        GetAsync<ConversationDto>($"agents/conversations/{conversationId}", cancellationToken);

    public Task<PendingActionDto> ApproveActionAsync(Guid actionId, CancellationToken cancellationToken) =>
        SendAsync<PendingActionDto>(HttpMethod.Post, $"agents/actions/{actionId}/approve", cancellationToken);

    public Task<PendingActionDto> RejectActionAsync(Guid actionId, CancellationToken cancellationToken) =>
        SendAsync<PendingActionDto>(HttpMethod.Post, $"agents/actions/{actionId}/reject", cancellationToken);

    public Task<IReadOnlyList<AgentUsageDto>> GetUsageByAgentAsync(DateTime fromUtc, CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<AgentUsageDto>>(
            $"aiengine/usage/by-agent?fromUtc={Uri.EscapeDataString(fromUtc.ToString("O", CultureInfo.InvariantCulture))}",
            cancellationToken);

    private Task<T> GetAsync<T>(string path, CancellationToken cancellationToken) =>
        SendAsync<T>(HttpMethod.Get, path, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri("api/" + path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAccessTokenAsync(cancellationToken));

        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new PricelyApiException(response.StatusCode, null, "The API returned an empty response.");
    }

    /// <summary>Turns a refused call into an exception carrying the error code of the problem details.</summary>
    internal static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new SessionExpiredException();
        }

        ProblemDto? problem = null;
        if (string.Equals(response.Content.Headers.ContentType?.MediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                problem = await response.Content.ReadFromJsonAsync<ProblemDto>(JsonOptions, cancellationToken);
            }
            catch (JsonException)
            {
                // Not a readable problem: the status code alone describes it.
            }
        }

        throw new PricelyApiException(
            response.StatusCode,
            problem?.Title,
            problem?.Detail ?? $"The API answered {(int)response.StatusCode} ({response.ReasonPhrase}).");
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private sealed record ProblemDto(string? Title, string? Detail);
}
