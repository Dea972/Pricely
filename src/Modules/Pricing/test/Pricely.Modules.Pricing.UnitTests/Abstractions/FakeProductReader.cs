using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Products;

namespace Pricely.Modules.Pricing.UnitTests.Abstractions;

/// <summary>
/// Catalog's products as the contract exposes them, in memory. <see cref="Failure"/> makes every call fail, as an
/// unreachable Catalog would.
/// </summary>
internal sealed class FakeProductReader(params ProductSummary[] products) : IProductReader
{
    public Error? Failure { get; init; }

    public Task<Result<ProductSummary?>> GetAsync(Guid productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failure is { } failure
            ? Result.Failure<ProductSummary?>(failure)
            : Result.Success(Array.Find(products, p => p.Id == productId)));

    public Task<Result<IReadOnlyList<ProductSummary>>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Failure is { } failure
            ? Result.Failure<IReadOnlyList<ProductSummary>>(failure)
            : Result.Success<IReadOnlyList<ProductSummary>>([.. products.Where(p => productIds.Contains(p.Id))]));

    public Task<Result<IReadOnlyList<Guid>>> GetIdsPageAsync(
        Guid? afterProductId,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Failure is { } failure
            ? Result.Failure<IReadOnlyList<Guid>>(failure)
            : Result.Success<IReadOnlyList<Guid>>([.. products.Select(p => p.Id).Order().Where(id => afterProductId is null || id.CompareTo(afterProductId.Value) > 0).Take(pageSize)]));
}
