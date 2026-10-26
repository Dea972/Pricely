using Pricely.Common.Application.Clock;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Application.PageSnapshots;

/// <summary>
/// Reads a page of a merchant's site and keeps its snapshot (ADR-009): the address, and every redirect, must stay on
/// that site. Used by the <c>fetch_merchant_page</c> tool and by the collection agent.
/// </summary>
public sealed class MerchantPageReader(
    IMerchantRepository merchantRepository,
    IMerchantPageFetcher pageFetcher,
    IPageSnapshotRepository pageSnapshotRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    private const int MaxRedirects = 3;

    /// <param name="address">The page to read, or null to open the merchant's search page for <paramref name="searchText"/>.</param>
    public async Task<Result<PageSnapshot>> ReadAsync(
        Guid merchantId,
        Uri? address,
        string? searchText,
        CancellationToken cancellationToken)
    {
        Merchant? merchant = await merchantRepository.GetAsync(merchantId, cancellationToken);
        if (merchant is null)
        {
            return Result.Failure<PageSnapshot>(MerchantErrors.NotFound(merchantId));
        }

        Uri? target = address ?? (string.IsNullOrWhiteSpace(searchText) ? null : merchant.SearchAddress(searchText));
        if (target is null)
        {
            return Result.Failure<PageSnapshot>(PageSnapshotErrors.NoAddress(merchantId));
        }

        for (int redirects = 0; redirects <= MaxRedirects; redirects++)
        {
            if (!merchant.IsActive)
            {
                return Result.Failure<PageSnapshot>(MerchantErrors.Inactive(merchantId));
            }

            if (!merchant.Owns(target))
            {
                return Result.Failure<PageSnapshot>(MerchantErrors.AddressOutsideMerchant(target));
            }

            Result<FetchedPage> page = await pageFetcher.FetchAsync(target, cancellationToken);
            if (page.IsFailure)
            {
                return Result.Failure<PageSnapshot>(page.Error);
            }

            if (page.Value.RedirectTo is not Uri next)
            {
                return await KeepAsync(merchant, page.Value, cancellationToken);
            }

            target = next;
        }

        return Result.Failure<PageSnapshot>(PageSnapshotErrors.TooManyRedirects(target));
    }

    private async Task<Result<PageSnapshot>> KeepAsync(Merchant merchant, FetchedPage page, CancellationToken cancellationToken)
    {
        Result<PageSnapshot> snapshot = PageSnapshot.Capture(merchant, page.Address, page.Text, dateTimeProvider.UtcNow);
        if (snapshot.IsFailure)
        {
            return snapshot;
        }

        pageSnapshotRepository.Insert(snapshot.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return snapshot;
    }
}
