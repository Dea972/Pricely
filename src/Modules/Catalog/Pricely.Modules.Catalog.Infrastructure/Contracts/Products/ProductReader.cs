using Microsoft.EntityFrameworkCore;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Products;
using Pricely.Modules.Catalog.Domain.Products;
using Pricely.Modules.Catalog.Infrastructure.Database;

namespace Pricely.Modules.Catalog.Infrastructure.Contracts.Products;

/// <summary>
/// Implements <see cref="IProductReader"/> on the Catalog database: a projection of the products, no business rule.
/// </summary>
internal sealed class ProductReader(CatalogDbContext context) : IProductReader
{
    public async Task<Result<ProductSummary?>> GetAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        ProductSummary? product = (await Read(context.Products.Where(p => p.Id == productId), cancellationToken)).FirstOrDefault();

        return Result.Success(product);
    }

    public async Task<Result<IReadOnlyList<ProductSummary>>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count > IProductReader.MaxIds)
        {
            return Result.Failure<IReadOnlyList<ProductSummary>>(ProductReaderErrors.TooManyIds);
        }

        Guid[] ids = [.. productIds.Distinct()];
        List<ProductSummary> products = await Read(context.Products.Where(p => ids.Contains(p.Id)), cancellationToken);

        return Result.Success<IReadOnlyList<ProductSummary>>(products);
    }

    public async Task<Result<IReadOnlyList<Guid>>> GetIdsPageAsync(
        Guid? afterProductId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageSize is < 1 or > IProductReader.MaxPageSize)
        {
            return Result.Failure<IReadOnlyList<Guid>>(ProductReaderErrors.InvalidPageSize);
        }

        IQueryable<Product> products = context.Products.AsNoTracking();
        if (afterProductId is Guid after)
        {
            products = products.Where(p => p.Id.CompareTo(after) > 0);
        }

        List<Guid> ids = await products.OrderBy(p => p.Id).Select(p => p.Id).Take(pageSize).ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<Guid>>(ids);
    }

    private static async Task<List<ProductSummary>> Read(IQueryable<Product> products, CancellationToken cancellationToken)
    {
        // The GTIN is a value object stored as text: it is converted to its text after the read.
        var rows = await products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Brand, p.Gtin, p.ManufacturerReference, p.ProductCategoryId })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(r => new ProductSummary(r.Id, r.Name, r.Brand, r.Gtin?.Value, r.ManufacturerReference, r.ProductCategoryId))];
    }
}
