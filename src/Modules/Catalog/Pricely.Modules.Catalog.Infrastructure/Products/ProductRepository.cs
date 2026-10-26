using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Catalog.Domain.Products;
using Pricely.Modules.Catalog.Infrastructure.Database;

namespace Pricely.Modules.Catalog.Infrastructure.Products;

internal sealed class ProductRepository(CatalogDbContext context) : IProductRepository
{
    public Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsWithGtinAsync(
        Gtin gtin,
        Guid? excludedProductId,
        CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(
            p => p.Gtin == gtin && (excludedProductId == null || p.Id != excludedProductId),
            cancellationToken);

    public Task<Product?> FindByGtinAsync(Gtin gtin, CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Gtin == gtin, cancellationToken);

    public void Insert(Product product) => context.Products.Add(product);
}
