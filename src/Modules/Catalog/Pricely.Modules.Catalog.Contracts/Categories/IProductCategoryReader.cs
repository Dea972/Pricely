using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Contracts.Categories;

/// <summary>
/// Reads the product typologies of Catalog. Read only: the other modules ask Catalog instead of keeping a copy of its tree.
/// The tree is small (three levels at most), so the calls are bounded.
/// </summary>
public interface IProductCategoryReader
{
    /// <summary>The typology, or null when Catalog does not know it.</summary>
    Task<Result<ProductCategorySummary?>> GetAsync(Guid productCategoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The typology followed by its ancestors up to the root, nearest first; empty when Catalog does not know the typology.
    /// </summary>
    Task<Result<IReadOnlyList<ProductCategorySummary>>> GetLineageAsync(
        Guid productCategoryId,
        CancellationToken cancellationToken = default);

    /// <summary>Every typology, roots first, then by name.</summary>
    Task<Result<IReadOnlyList<ProductCategorySummary>>> GetAllAsync(CancellationToken cancellationToken = default);
}
