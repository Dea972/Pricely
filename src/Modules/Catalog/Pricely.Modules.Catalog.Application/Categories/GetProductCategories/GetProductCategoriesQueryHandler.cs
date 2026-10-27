using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Application.Categories.GetProductCategories;

internal sealed class GetProductCategoriesQueryHandler(IProductCategoryReadStore readStore)
    : IQueryHandler<GetProductCategoriesQuery, IReadOnlyCollection<ProductCategoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<ProductCategoryResponse>>> Handle(
        GetProductCategoriesQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetAllAsync(cancellationToken));
}
