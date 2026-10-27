using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pricely.Common.Application.AI.Agents;
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
        string providerName = string.Equals(settings.Provider, WebSearchProviders.Brave, StringComparison.OrdinalIgnoreCase)
            ? WebSearchProviders.Brave
            : WebSearchProviders.Anthropic;

        // Brave bills one search per request; the model behind Anthropic may run up to MaxSearchesPerRequest.
        int wanted = providerName == WebSearchProviders.Brave ? 1 : Math.Max(1, settings.MaxSearchesPerRequest);
        DateTime searchedOnUtc = dateTimeProvider.UtcNow;
        int reserved = quota.Reserve(searchedOnUtc, settings.MaxSearchesPerDay, wanted);
        if (reserved == 0)
        {
            return Result.Failure<WebSearchResults>(WebSearchErrors.DailyLimitReached(settings.MaxSearchesPerDay));
        }

        IWebSearchProvider provider = services.GetRequiredKeyedService<IWebSearchProvider>(providerName);

        // Inside an agent run, the search belongs to the conversation (its cost is the conversation's).
        AgentToolCall? agentCall = AgentToolCall.Current;
        WebSearchRequest bounded = request with
        {
            UserId = request.UserId ?? agentCall?.UserId,
            Query = request.Query.Trim().Length > WebSearchRequest.MaxQueryLength ? request.Query.Trim()[..WebSearchRequest.MaxQueryLength] : request.Query.Trim(),
            MaxResults = Math.Clamp(request.MaxResults, 1, 20)
        };
        string sessionId = request.SessionId ?? agentCall?.SessionId ?? $"web-search:{Guid.CreateVersion7()}";

        var stopwatch = Stopwatch.StartNew();
        WebSearchResults? results = null;
        string? errorMessage = null;
        int billed = 0;

        try
        {
            results = await provider.SearchAsync(bounded, sessionId, cancellationToken);
            billed = results.Searches;
        }
        catch (WebSearchFailedException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Searches run before the failure are billed all the same.
            errorMessage = exception.Message;
            billed = exception.BilledSearches;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            errorMessage = exception.Message;
        }
        finally
        {
            stopwatch.Stop();
            quota.Settle(searchedOnUtc, reserved, billed);
        }

        await eventBus.PublishAsync(
            new WebSearchPerformedIntegrationEvent(Guid.CreateVersion7(), searchedOnUtc)
            {
                Provider = provider.Name,
                SessionId = sessionId,
                UserId = bounded.UserId,
                Searches = billed,
                Results = results?.Results.Count ?? 0,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = errorMessage is null,
                ErrorMessage = errorMessage
            },
            cancellationToken);

        // The detail of a failure (a provider's error body) stays in the journal, never in the answer.
        return results is null
            ? Result.Failure<WebSearchResults>(WebSearchErrors.Failed(provider.Name))
            : results;
    }
}
