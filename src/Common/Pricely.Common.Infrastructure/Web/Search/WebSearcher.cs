using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;

namespace Pricely.Common.Infrastructure.Web.Search;

/// <summary>
/// <see cref="IWebSearcher"/>: the provider of the configuration, the daily cap, and the journal of every search
/// (<see cref="WebSearchPerformedIntegrationEvent"/>).
/// </summary>
internal sealed class WebSearcher(
    IServiceProvider services,
    IOptionsMonitor<WebSearchOptions> options,
    WebSearchQuota quota,
    IEventBus eventBus,
    IDateTimeProvider dateTimeProvider)
    : IWebSearcher
{
    public async Task<Result<WebSearchResults>> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Result.Failure<WebSearchResults>(WebSearchErrors.EmptyQuery);
        }

        WebSearchOptions settings = options.CurrentValue;
        if (!quota.HasRoom(dateTimeProvider.UtcNow, settings.MaxSearchesPerDay))
        {
            return Result.Failure<WebSearchResults>(WebSearchErrors.DailyLimitReached(settings.MaxSearchesPerDay));
        }

        string providerName = string.Equals(settings.Provider, WebSearchProviders.Brave, StringComparison.OrdinalIgnoreCase)
            ? WebSearchProviders.Brave
            : WebSearchProviders.Anthropic;
        IWebSearchProvider provider = services.GetRequiredKeyedService<IWebSearchProvider>(providerName);

        WebSearchRequest bounded = request with
        {
            Query = request.Query.Trim().Length > WebSearchRequest.MaxQueryLength ? request.Query.Trim()[..WebSearchRequest.MaxQueryLength] : request.Query.Trim(),
            MaxResults = Math.Clamp(request.MaxResults, 1, 20)
        };
        string sessionId = request.SessionId ?? $"web-search:{Guid.CreateVersion7()}";

        DateTime searchedOnUtc = dateTimeProvider.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        WebSearchResults? results = null;
        string? errorMessage = null;

        try
        {
            results = await provider.SearchAsync(bounded, sessionId, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            errorMessage = exception.Message;
        }

        stopwatch.Stop();
        quota.Count(searchedOnUtc, results?.Searches ?? 0);

        await eventBus.PublishAsync(
            new WebSearchPerformedIntegrationEvent(Guid.CreateVersion7(), searchedOnUtc)
            {
                Provider = provider.Name,
                SessionId = sessionId,
                UserId = request.UserId,
                Searches = results?.Searches ?? 0,
                Results = results?.Results.Count ?? 0,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = errorMessage is null,
                ErrorMessage = errorMessage
            },
            cancellationToken);

        return results is null
            ? Result.Failure<WebSearchResults>(WebSearchErrors.Failed(provider.Name, errorMessage ?? "no result"))
            : results;
    }
}
