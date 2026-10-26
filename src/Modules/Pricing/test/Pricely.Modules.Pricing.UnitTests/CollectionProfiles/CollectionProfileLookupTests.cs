using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;
using Pricely.Modules.Pricing.Application.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.UnitTests.Abstractions;

namespace Pricely.Modules.Pricing.UnitTests.CollectionProfiles;

/// <summary>The lineage comes from the Catalog contract; the inheritance rules themselves are those of the resolver.</summary>
public sealed class CollectionProfileLookupTests
{
    private static readonly ProductCategorySummary Root = new(Guid.NewGuid(), "Gros œuvre", null, 1);
    private static readonly ProductCategorySummary Parent = new(Guid.NewGuid(), "Ciment", Root.Id, 2);
    private static readonly ProductCategorySummary Leaf = new(Guid.NewGuid(), "Ciment prompt", Parent.Id, 3);

    [Fact]
    public async Task Resolve_ShouldInheritFromTheAncestorsCatalogReports()
    {
        var rootMerchant = Guid.NewGuid();
        var profiles = new FakeProfiles(CollectionProfile.Define(Root.Id, [rootMerchant], "Prix TTC.").Value);

        Result<ResolvedCollectionProfile> result = await CollectionProfileLookup.ResolveAsync(
            Leaf.Id,
            new FakeProductCategoryReader(Root, Parent, Leaf),
            profiles,
            CancellationToken.None);

        result.Value.PreferredMerchantIds.Should().Equal(rootMerchant);
        result.Value.PreferredMerchantsSourceCategoryId.Should().Be(Root.Id);
        result.Value.Instructions.Should().Be("Prix TTC.");
    }

    [Fact]
    public async Task Resolve_ShouldFail_WhenCatalogDoesNotKnowTheTypology()
    {
        Result<ResolvedCollectionProfile> result = await CollectionProfileLookup.ResolveAsync(
            Guid.NewGuid(),
            new FakeProductCategoryReader(Root, Parent, Leaf),
            new FakeProfiles(),
            CancellationToken.None);

        result.Error.Code.Should().Be("Catalog.CategoryNotFound");
    }

    [Fact]
    public async Task Resolve_ShouldPassOnTheFailure_WhenCatalogCannotBeRead()
    {
        var unreachable = Error.Failure("Catalog.Unreachable", "Catalog does not answer.");

        Result<ResolvedCollectionProfile> result = await CollectionProfileLookup.ResolveAsync(
            Leaf.Id,
            new FakeProductCategoryReader(Root, Parent, Leaf) { Failure = unreachable },
            new FakeProfiles(),
            CancellationToken.None);

        result.Error.Should().Be(unreachable);
    }

    private sealed class FakeProfiles(params CollectionProfile[] profiles) : ICollectionProfileRepository
    {
        public Task<CollectionProfile?> GetByCategoryAsync(Guid productCategoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Find(profiles, p => p.ProductCategoryId == productCategoryId));

        public Task<IReadOnlyCollection<CollectionProfile>> GetByCategoriesAsync(IReadOnlyCollection<Guid> productCategoryIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CollectionProfile>>([.. profiles.Where(p => productCategoryIds.Contains(p.ProductCategoryId))]);

        public void Insert(CollectionProfile profile) => throw new NotSupportedException();
    }
}
