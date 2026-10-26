using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
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
    IWebPageReader webPageReader,
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

        // A search that the site redirects to another page (an exact match) lands on that page, not on a result list.
        Uri? searchPage = address is null ? target : null;

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

            Result<WebPage> page = await webPageReader.ReadAsync(target, cancellationToken);
            if (page.IsFailure)
            {
                return Result.Failure<PageSnapshot>(page.Error);
            }

            if (page.Value.RedirectTo is not Uri next)
            {
                return await KeepAsync(merchant, page.Value, page.Value.Address == searchPage, cancellationToken);
            }

            target = next;
        }

        return Result.Failure<PageSnapshot>(WebErrors.TooManyRedirects(target));
    }

    /// <summary>
    /// Saves the snapshot alone (one aggregate per save, like <c>CollectedOfferRecorder</c>): the agent reads pages
    /// during a collection, before any offer is recorded.
    /// </summary>
    private async Task<Result<PageSnapshot>> KeepAsync(Merchant merchant, WebPage page, bool isSearchPage, CancellationToken cancellationToken)
    {
        Result<PageSnapshot> snapshot = PageSnapshot.Capture(merchant, page.Address, page.Text, dateTimeProvider.UtcNow, isSearchPage);
        if (snapshot.IsFailure)
        {
            return snapshot;
        }

        pageSnapshotRepository.Insert(snapshot.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return snapshot;
    }
}
