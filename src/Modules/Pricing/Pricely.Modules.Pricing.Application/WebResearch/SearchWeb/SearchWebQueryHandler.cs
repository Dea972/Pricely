using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Merchants;

namespace Pricely.Modules.Pricing.Application.WebResearch.SearchWeb;

internal sealed class SearchWebQueryHandler(
    IWebSearcher webSearcher,
    IMerchantReadStore merchantReadStore,
    ISearchedPages searchedPages,
    ICurrentUser currentUser)
    : IQueryHandler<SearchWebQuery, WebSearchResponse>
{
    public async Task<Result<WebSearchResponse>> Handle(SearchWebQuery request, CancellationToken cancellationToken)
    {
        Result<WebSearchResults> results = await webSearcher.SearchAsync(
            new WebSearchRequest(request.Query) { UserId = currentUser.UserId?.ToString() },
            cancellationToken);
        if (results.IsFailure)
        {
            return Result.Failure<WebSearchResponse>(results.Error);
        }

        searchedPages.Remember(WebResearchScope.Of(currentUser), results.Value.Results.Select(r => r.Address));
        KnownMerchants merchants = await KnownMerchants.LoadAsync(merchantReadStore, cancellationToken);

        return new WebSearchResponse(
            results.Value.Provider,
            [.. results.Value.Results.Select(r => new WebSearchHit(r.Title, r.Address, r.Snippet, r.PageAge, merchants.Find(r.Address)))],
            WebSearchResponse.UnverifiedNotice);
    }
}
