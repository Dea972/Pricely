using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Merchants;

namespace Pricely.Modules.Pricing.Application.WebResearch.ReadWebPage;

internal sealed class ReadWebPageQueryHandler(
    IWebPageReader webPageReader,
    IMerchantReadStore merchantReadStore,
    ISearchedPages searchedPages,
    ICurrentUser currentUser)
    : IQueryHandler<ReadWebPageQuery, WebPageResponse>
{
    public async Task<Result<WebPageResponse>> Handle(ReadWebPageQuery request, CancellationToken cancellationToken)
    {
        // Only a page a search of this research returned, or a page of a followed merchant: never an address a page
        // made up (ADR-027).
        KnownMerchants merchants = await KnownMerchants.LoadAsync(merchantReadStore, cancellationToken);
        if (merchants.Find(request.Address) is null && !searchedPages.WasFound(WebResearchScope.Of(currentUser), request.Address))
        {
            return Result.Failure<WebPageResponse>(WebResearchErrors.NotFoundBySearch(request.Address));
        }

        // Any public page may be read (the reader refuses private addresses), redirects included, but only over http(s) on its default port.
        Result<WebPage> page = await webPageReader.ReadFollowingAsync(request.Address, IsWebAddress, cancellationToken);
        if (page.IsFailure)
        {
            return Result.Failure<WebPageResponse>(page.Error);
        }

        string text = page.Value.Text;

        return new WebPageResponse(
            page.Value.Address,
            page.Value.Title,
            text.Length > WebPageResponse.MaxTextLength ? text[..WebPageResponse.MaxTextLength] : text,
            text.Length > WebPageResponse.MaxTextLength,
            [.. page.Value.Products.Take(WebPageResponse.MaxProducts)],
            merchants.Find(page.Value.Address),
            WebPageResponse.UnverifiedNotice);
    }

    /// <summary>An http(s) address on the default port (80 or 443) without credentials: never another service of a host.</summary>
    internal static bool IsWebAddress(Uri address) =>
        address.IsAbsoluteUri
        && address.Scheme is "http" or "https"
        && address.IsDefaultPort
        && string.IsNullOrEmpty(address.UserInfo);
}
