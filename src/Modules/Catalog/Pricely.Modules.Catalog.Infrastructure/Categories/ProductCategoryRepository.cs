using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Infrastructure.Database;

namespace Pricely.Modules.Catalog.Infrastructure.Categories;

internal sealed class ProductCategoryRepository(CatalogDbContext context) : IProductCategoryRepository
{
    public Task<ProductCategory?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Insert(ProductCategory category) => context.ProductCategories.Add(category);
}
