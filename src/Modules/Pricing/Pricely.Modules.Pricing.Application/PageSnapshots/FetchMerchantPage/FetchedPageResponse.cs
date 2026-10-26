using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Application.PageSnapshots.FetchMerchantPage;

/// <param name="SnapshotId">To give back with a submitted offer: its price must appear in this page.</param>
/// <param name="Text">The text of the page, cut after <see cref="MaxTextLength"/> characters for the model.</param>
public sealed record FetchedPageResponse(Guid SnapshotId, Guid MerchantId, Uri Address, string Text, bool IsTruncated)
{
    public const int MaxTextLength = 20_000;

    public static FetchedPageResponse From(PageSnapshot snapshot) =>
        new(
            snapshot.Id,
            snapshot.MerchantId,
            snapshot.Address,
            snapshot.Content.Length > MaxTextLength ? snapshot.Content[..MaxTextLength] : snapshot.Content,
            snapshot.Content.Length > MaxTextLength);
}
