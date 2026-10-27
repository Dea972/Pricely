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
        // made up (ADR-027). Every redirect is held to the same rule, so that an open redirect of a site does not lead
        // elsewhere.
        KnownMerchants merchants = await KnownMerchants.LoadAsync(merchantReadStore, cancellationToken);
        string research = WebResearchScope.Of(currentUser);
        KnownMerchant? merchant = merchants.Find(request.Address);
        if (merchant is null && !searchedPages.WasFound(research, request.Address))
        {
            return Result.Failure<WebPageResponse>(WebResearchErrors.NotFoundBySearch(request.Address));
        }

        bool IsAdmitted(Uri next) =>
            IsWebAddress(next)
            && (merchant is not null
                ? merchants.Find(next)?.Id == merchant.Id
                : SameHost(next, request.Address) || searchedPages.WasFound(research, next));

        Result<WebPage> page = await webPageReader.ReadFollowingAsync(request.Address, IsAdmitted, cancellationToken);
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

    /// <summary>The same host, with or without <c>www.</c> (a site redirecting to its canonical address).</summary>
    private static bool SameHost(Uri next, Uri address) =>
        string.Equals(WithoutWww(next.IdnHost), WithoutWww(address.IdnHost), StringComparison.OrdinalIgnoreCase);

    private static string WithoutWww(string host) =>
        host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;

    /// <summary>An http(s) address on the default port (80 or 443) without credentials: never another service of a host.</summary>
    internal static bool IsWebAddress(Uri address) =>
        address.IsAbsoluteUri
        && address.Scheme is "http" or "https"
        && address.IsDefaultPort
        && string.IsNullOrEmpty(address.UserInfo);
}
