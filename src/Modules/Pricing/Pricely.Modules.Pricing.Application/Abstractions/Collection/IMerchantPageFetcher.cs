using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Application.Abstractions.Collection;

/// <summary>
/// Port to the merchant sites: reads one page and returns its text, never following a redirect itself (the caller
/// checks that the target is still on the merchant's site). Implemented in Infrastructure.
/// </summary>
public interface IMerchantPageFetcher
{
    Task<Result<FetchedPage>> FetchAsync(Uri address, CancellationToken cancellationToken = default);
}

/// <param name="Address">The address that was read.</param>
/// <param name="Text">The readable text of the page (markup removed, structured product data kept).</param>
/// <param name="RedirectTo">Where the site redirects, when it answers with a redirect instead of a page.</param>
public sealed record FetchedPage(Uri Address, string Text, Uri? RedirectTo);
