using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Application.WebResearch;

public static class WebResearchErrors
{
    public static Error NotFoundBySearch(Uri address) =>
        Error.Problem(
            "WebResearch.NotFoundBySearch",
            $"The page {address} was not returned by a web search of this conversation and is not on a followed merchant's site: search for it first");
}
