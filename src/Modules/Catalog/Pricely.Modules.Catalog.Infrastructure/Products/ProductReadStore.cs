using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Catalog.Application.Products;
using Pricely.Modules.Catalog.Application.Products.GetProduct;
using Pricely.Modules.Catalog.Domain.Products;
using Pricely.Modules.Catalog.Infrastructure.Database;

namespace Pricely.Modules.Catalog.Infrastructure.Products;

internal sealed class ProductReadStore(CatalogDbContext context) : IProductReadStore
{
    public async Task<ProductResponse?> GetAsync(Guid productId, CancellationToken cancellationToken = default) =>
        (await ReadAsync(context.Products.Where(p => p.Id == productId), cancellationToken)).FirstOrDefault();

    public async Task<IReadOnlyCollection<ProductResponse>> SearchAsync(
        string? text,
        Guid? productCategoryId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> products = context.Products;

        if (text is not null)
        {
            string pattern = $"%{LikePattern.Escape(text)}%";
            Gtin? gtin = Gtin.Create(text) is { IsSuccess: true } created ? created.Value : null;

            products = products.Where(p =>
                EF.Functions.ILike(p.Name, pattern)
                || EF.Functions.ILike(p.Brand, pattern)
                || p.ManufacturerReference != null && EF.Functions.ILike(p.ManufacturerReference, pattern)
                || gtin != null && p.Gtin == gtin);
        }

        if (productCategoryId is Guid categoryId)
        {
            products = products.Where(p => p.ProductCategoryId == categoryId);
        }

        return await ReadAsync(products.OrderBy(p => p.Name).Take(limit), cancellationToken);
    }

    private static async Task<List<ProductResponse>> ReadAsync(IQueryable<Product> products, CancellationToken cancellationToken)
    {
        // The GTIN is a value object stored as text: it is converted to its text after the read.
        var rows = await products
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.Brand, p.Gtin, p.ManufacturerReference, p.ProductCategoryId })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(r => new ProductResponse(r.Id, r.Name, r.Brand, r.Gtin?.Value, r.ManufacturerReference, r.ProductCategoryId))];
    }
}
