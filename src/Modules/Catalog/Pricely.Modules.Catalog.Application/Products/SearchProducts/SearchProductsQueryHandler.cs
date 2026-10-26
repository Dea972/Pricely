using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Products.GetProduct;

namespace Pricely.Modules.Catalog.Application.Products.SearchProducts;

internal sealed class SearchProductsQueryHandler(IProductReadStore readStore)
    : IQueryHandler<SearchProductsQuery, IReadOnlyCollection<ProductResponse>>
{
    public async Task<Result<IReadOnlyCollection<ProductResponse>>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        string? text = string.IsNullOrWhiteSpace(request.Text) ? null : request.Text.Trim();

        return Result.Success(await readStore.SearchAsync(text, request.ProductCategoryId, request.Limit, cancellationToken));
    }
}
