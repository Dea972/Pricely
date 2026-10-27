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

    public async Task<Product?> FindByEquivalentGtinAsync(Gtin gtin, CancellationToken cancellationToken = default)
    {
        List<Gtin> forms = [.. gtin.EquivalentForms().Select(f => Gtin.Create(f).Value)];

        return await context.Products.AsNoTracking().FirstOrDefaultAsync(p => forms.Contains(p.Gtin!), cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByBrandAsync(string brand, CancellationToken cancellationToken = default)
    {
        string pattern = LikePattern.Escape(brand.Trim());

        return await context.Products.AsNoTracking().Where(p => EF.Functions.ILike(p.Brand, pattern)).Take(500).ToListAsync(cancellationToken);
    }

    public void Insert(Product product) => context.Products.Add(product);
}
