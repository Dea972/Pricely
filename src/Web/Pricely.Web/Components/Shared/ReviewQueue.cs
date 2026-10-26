using Pricely.Web.Api;

namespace Pricely.Web.Components.Shared;

/// <summary>
/// The number of offers waiting for a review, for the badge of the navigation (one per circuit). Refreshed when a screen
/// confirms or rejects an offer; a failed call leaves the badge as it was rather than showing an error.
/// </summary>
internal sealed class ReviewQueue(PricelyApiClient api)
{
    public int? Count { get; private set; }

    public event Action? Changed;

    public async Task RefreshAsync()
    {
        try
        {
            IReadOnlyList<PendingOfferDto> pending = await api.GetOffersPendingReviewAsync(CancellationToken.None);
            Set(pending.Count);
        }
        catch (Exception exception) when (ApiProblem.From(exception, string.Empty) is not null)
        {
            // The badge is secondary: the screens say what failed.
        }
    }

    public void Set(int count)
    {
        Count = count;
        Changed?.Invoke();
    }
}
