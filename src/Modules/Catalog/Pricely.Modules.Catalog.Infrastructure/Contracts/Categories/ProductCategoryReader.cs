using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Infrastructure.Database;

namespace Pricely.Modules.Catalog.Infrastructure.Contracts.Categories;

/// <summary>
/// Implements <see cref="IProductCategoryReader"/> on the Catalog database: a projection of the typologies, no business rule.
/// Not a MediatR query: it is a technical read for other modules, not a use case of Catalog.
/// </summary>
internal sealed class ProductCategoryReader(CatalogDbContext context) : IProductCategoryReader
{
    private static readonly Expression<Func<ProductCategory, ProductCategorySummary>> ToSummary =
        c => new ProductCategorySummary(c.Id, c.Name, c.ParentId, c.Depth);

    public async Task<Result<ProductCategorySummary?>> GetAsync(Guid productCategoryId, CancellationToken cancellationToken = default)
    {
        ProductCategorySummary? category = await FindAsync(productCategoryId, cancellationToken);

        return Result.Success(category);
    }

    public async Task<Result<IReadOnlyList<ProductCategorySummary>>> GetLineageAsync(
        Guid productCategoryId,
        CancellationToken cancellationToken = default)
    {
        List<ProductCategorySummary> lineage = [];
        Guid? currentId = productCategoryId;

        // The tree has MaxDepth levels at most: the bound only guards against a cycle in bad data.
        while (currentId is Guid id && lineage.Count < ProductCategory.MaxDepth)
        {
            ProductCategorySummary? category = await FindAsync(id, cancellationToken);
            if (category is null)
            {
                break;
            }

            lineage.Add(category);
            currentId = category.ParentId;
        }

        return Result.Success<IReadOnlyList<ProductCategorySummary>>(lineage);
    }

    public async Task<Result<IReadOnlyList<ProductCategorySummary>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<ProductCategorySummary> categories = await context.ProductCategories
            .AsNoTracking()
            .OrderBy(c => c.Depth)
            .ThenBy(c => c.Name)
            .Select(ToSummary)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ProductCategorySummary>>(categories);
    }

    private Task<ProductCategorySummary?> FindAsync(Guid productCategoryId, CancellationToken cancellationToken) =>
        context.ProductCategories
            .AsNoTracking()
            .Where(c => c.Id == productCategoryId)
            .Select(ToSummary)
            .FirstOrDefaultAsync(cancellationToken);
}
