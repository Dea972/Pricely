using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;

namespace Pricely.Modules.Pricing.UnitTests.Abstractions;

/// <summary>
/// Catalog's typologies as the contract exposes them, in memory. <see cref="Failure"/> makes every call fail, as an
/// unreachable Catalog would.
/// </summary>
internal sealed class FakeProductCategoryReader(params ProductCategorySummary[] categories) : IProductCategoryReader
{
    public Error? Failure { get; init; }

    public Task<Result<ProductCategorySummary?>> GetAsync(Guid productCategoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failure is { } failure
            ? Result.Failure<ProductCategorySummary?>(failure)
            : Result.Success(Find(productCategoryId)));

    public Task<Result<IReadOnlyList<ProductCategorySummary>>> GetLineageAsync(
        Guid productCategoryId,
        CancellationToken cancellationToken = default)
    {
        if (Failure is { } failure)
        {
            return Task.FromResult(Result.Failure<IReadOnlyList<ProductCategorySummary>>(failure));
        }

        List<ProductCategorySummary> lineage = [];
        ProductCategorySummary? current = Find(productCategoryId);
        while (current is not null)
        {
            lineage.Add(current);
            current = current.ParentId is Guid parentId ? Find(parentId) : null;
        }

        return Task.FromResult(Result.Success<IReadOnlyList<ProductCategorySummary>>(lineage));
    }

    public Task<Result<IReadOnlyList<ProductCategorySummary>>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Failure is { } failure
            ? Result.Failure<IReadOnlyList<ProductCategorySummary>>(failure)
            : Result.Success<IReadOnlyList<ProductCategorySummary>>(categories));

    private ProductCategorySummary? Find(Guid id) => Array.Find(categories, c => c.Id == id);
}
