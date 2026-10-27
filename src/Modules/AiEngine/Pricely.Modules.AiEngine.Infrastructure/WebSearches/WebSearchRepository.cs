using Pricely.Modules.AiEngine.Domain.WebSearches;
using Pricely.Modules.AiEngine.Infrastructure.Database;

namespace Pricely.Modules.AiEngine.Infrastructure.WebSearches;

internal sealed class WebSearchRepository(AiEngineDbContext context) : IWebSearchRepository
{
    public void Insert(WebSearch search) => context.WebSearches.Add(search);
}
