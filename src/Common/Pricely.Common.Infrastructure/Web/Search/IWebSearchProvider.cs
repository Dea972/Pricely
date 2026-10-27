using Pricely.Common.Application.Web;

namespace Pricely.Common.Infrastructure.Web.Search;

/// <summary>One search engine behind <see cref="WebSearcher"/>. Throws on failure; the searcher turns it into a result.</summary>
internal interface IWebSearchProvider
{
    string Name { get; }

    Task<WebSearchResults> SearchAsync(WebSearchRequest request, string sessionId, CancellationToken cancellationToken);
}
