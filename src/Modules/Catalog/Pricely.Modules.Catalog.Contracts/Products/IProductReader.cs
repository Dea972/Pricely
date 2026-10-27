using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Contracts.Products;

/// <summary>
/// Reads the reference products of Catalog. Read only: the other modules ask Catalog instead of keeping a copy of its products.
/// </summary>
public interface IProductReader
{
    /// <summary>Maximum number of identifiers per call of <see cref="GetByIdsAsync"/>.</summary>
    const int MaxIds = 1000;

    /// <summary>Maximum size of a page of <see cref="GetIdsPageAsync"/>.</summary>
    const int MaxPageSize = 1000;

    /// <summary>The product, or null when Catalog does not know it.</summary>
    Task<Result<ProductSummary?>> GetAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>The known products among the requested identifiers (an unknown identifier is simply absent), by name.</summary>
    Task<Result<IReadOnlyList<ProductSummary>>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);

    /// <summary>Identifiers of the products, ordered by identifier (cursor pagination), to walk the whole catalog.</summary>
    /// <param name="afterProductId">Last identifier of the previous page; null for the first page.</param>
    /// <param name="pageSize">Number of identifiers per page (1 to <see cref="MaxPageSize"/>).</param>
    Task<Result<IReadOnlyList<Guid>>> GetIdsPageAsync(
        Guid? afterProductId,
        int pageSize,
        CancellationToken cancellationToken = default);
}
