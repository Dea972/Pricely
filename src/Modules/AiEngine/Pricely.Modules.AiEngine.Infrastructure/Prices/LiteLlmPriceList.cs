using System.Text.Json;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Prices;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Infrastructure.Prices;

/// <summary>
/// The public price list of LiteLLM (anti-corruption layer): one JSON object per model, keyed by its identifier
/// (<c>claude-sonnet-5-5</c>) or <c>provider/identifier</c>, prices in USD per token, provider in <c>litellm_provider</c>.
/// </summary>
internal sealed class LiteLlmPriceList(HttpClient http, ModelPriceOptions options) : IPublishedPriceList
{
    private const decimal TokensPerMillion = 1_000_000m;

    private static readonly Error Unavailable = Error.Problem("Prices.ListUnavailable", "The LiteLLM price list could not be read.");

    public async Task<Result<IReadOnlyCollection<PublishedPrice>>> FindAsync(
        IReadOnlyCollection<PublishedPriceQuery> models,
        CancellationToken cancellationToken = default)
    {
        if (options.LiteLlmPriceList is null)
        {
            return Result.Failure<IReadOnlyCollection<PublishedPrice>>(Unavailable);
        }

        try
        {
            await using Stream stream = await http.GetStreamAsync(options.LiteLlmPriceList, cancellationToken);
            using JsonDocument list = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return Result.Success<IReadOnlyCollection<PublishedPrice>>([.. models.Select(m => Find(list.RootElement, m)).OfType<PublishedPrice>()]);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<IReadOnlyCollection<PublishedPrice>>(Unavailable);
        }
    }

    internal static PublishedPrice? Find(JsonElement list, PublishedPriceQuery model)
    {
        string provider = ProviderName(model.Provider);
        string[] keys = [model.ProviderModelId, $"{provider}/{model.ProviderModelId}"];

        foreach (string key in keys)
        {
            if (list.TryGetProperty(key, out JsonElement entry)
                && (!entry.TryGetProperty("litellm_provider", out JsonElement listed)
                    || string.Equals(listed.GetString(), provider, StringComparison.OrdinalIgnoreCase))
                && PerMillion(entry, "input_cost_per_token") is decimal input
                && PerMillion(entry, "output_cost_per_token") is decimal output)
            {
                return new PublishedPrice(
                    model.Provider,
                    model.ProviderModelId,
                    input,
                    PerMillion(entry, "cache_read_input_token_cost"),
                    PerMillion(entry, "cache_creation_input_token_cost"),
                    output);
            }
        }

        return null;
    }

    /// <summary>The name of the provider in LiteLLM (<c>litellm_provider</c> and key prefix).</summary>
    private static string ProviderName(Provider provider) =>
        provider switch
        {
            Provider.Anthropic => "anthropic",
            _ => provider.ToString()
        };

    private static decimal? PerMillion(JsonElement entry, string property) =>
        entry.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal perToken)
            ? decimal.Round(perToken * TokensPerMillion, 6)
            : null;
}
