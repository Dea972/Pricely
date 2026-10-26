using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Products;
using Pricely.Modules.Pricing.Application.Offers;
using Pricely.Modules.Pricing.Application.Offers.GetProductsToCollect;
using Pricely.Modules.Pricing.UnitTests.Abstractions;

namespace Pricely.Modules.Pricing.UnitTests.Offers;

/// <summary>Walking and batching the products of Catalog through its contract (cursor pages, bounded batches).</summary>
public sealed class CatalogReadingTests
{
    [Fact]
    public async Task ProductsToCollect_ShouldWalkEveryPage_EvenWhenTheLastOneIsFull()
    {
        ProductSummary[] products = [.. Enumerable.Range(0, IProductReader.MaxPageSize * 2).Select(_ => Summary())];

        Result<IReadOnlyCollection<Guid>> result = await new GetProductsToCollectQueryHandler(new FakeProductReader(products))
            .Handle(new GetProductsToCollectQuery(), CancellationToken.None);

        result.Value.Should().BeEquivalentTo(products.Select(p => p.Id));
    }

    [Fact]
    public async Task ProductsToCollect_ShouldPassOnTheFailure_WhenCatalogCannotBeRead()
    {
        var unreachable = Error.Failure("Catalog.Unreachable", "Catalog does not answer.");

        Result<IReadOnlyCollection<Guid>> result = await new GetProductsToCollectQueryHandler(new FakeProductReader(Summary()) { Failure = unreachable })
            .Handle(new GetProductsToCollectQuery(), CancellationToken.None);

        result.Error.Should().Be(unreachable);
    }

    [Fact]
    public async Task ProductLookup_ShouldReadBatchesWithinTheLimitOfTheContract()
    {
        ProductSummary[] products = [.. Enumerable.Range(0, IProductReader.MaxIds + 1).Select(_ => Summary())];
        var reader = new FakeProductReader(products);

        Result<Dictionary<Guid, ProductSummary>> result = await ProductLookup.LoadAsync(reader, products.Select(p => p.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("more identifiers than one call accepts are split in batches");
        result.Value.Should().HaveCount(products.Length);
    }

    private static ProductSummary Summary() => new(Guid.NewGuid(), "Produit", "Marque", null, null, Guid.NewGuid());
}
