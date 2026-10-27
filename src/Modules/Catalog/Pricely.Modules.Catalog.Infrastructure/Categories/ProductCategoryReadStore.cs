using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Catalog.Application.Categories;
using Pricely.Modules.Catalog.Application.Categories.GetProductCategories;
using Pricely.Modules.Catalog.Infrastructure.Database;

namespace Pricely.Modules.Catalog.Infrastructure.Categories;

internal sealed class ProductCategoryReadStore(CatalogDbContext context) : IProductCategoryReadStore
{
    public async Task<IReadOnlyCollection<ProductCategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.ProductCategories
            .AsNoTracking()
            .OrderBy(c => c.Depth)
            .ThenBy(c => c.Name)
            .Select(c => new ProductCategoryResponse(c.Id, c.Name, c.ParentId, c.Depth))
            .ToListAsync(cancellationToken);
}
