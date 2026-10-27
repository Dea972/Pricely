using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;
using Pricely.Modules.Catalog.Contracts.Products;
using Pricely.Modules.Pricing.Application.CollectionProfiles;
using Pricely.Modules.Pricing.Application.CollectionProfiles.GetCollectionProfiles;
using Pricely.Modules.Pricing.Application.Offers;
using Pricely.Modules.Pricing.Application.Offers.GetOffersPendingReview;
using Pricely.Modules.Pricing.Application.Offers.GetPriceAnomalies;
using Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;
using Pricely.Modules.Pricing.Application.Offers.GetProductPriceComparison;
using Pricely.Modules.Pricing.UnitTests.Abstractions;

namespace Pricely.Modules.Pricing.UnitTests.Offers;

/// <summary>Pricing reads its own rows, and the products and typologies they refer to from Catalog through its contracts.</summary>
public sealed class OfferQueryHandlerTests
{
    private static readonly ProductSummary Cement = new(Guid.NewGuid(), "Ciment 35 kg", "Cimenterie", "3760000100014", "CR-35", Guid.NewGuid());

    [Fact]
    public async Task PendingReview_ShouldAddTheProductAndTheComparisonOfTheMatchingPolicy()
    {
        var store = new FakeOfferReadStore
        {
            Pending = [new PendingOfferRow { OfferId = Guid.NewGuid(), ProductId = Cement.Id, MerchantName = "Comptoir", ObservedGtin = "3760000100014", ObservedBrand = "Autre" }]
        };

        Result<IReadOnlyCollection<PendingOfferResponse>> result = await new GetOffersPendingReviewQueryHandler(store, new FakeProductReader(Cement))
            .Handle(new GetOffersPendingReviewQuery(), CancellationToken.None);

        PendingOfferResponse offer = result.Value.Should().ContainSingle().Subject;
        offer.ProductName.Should().Be("Ciment 35 kg");
        offer.ProductBrand.Should().Be("Cimenterie");
        offer.SameGtin.Should().BeTrue();
        offer.SameBrand.Should().BeFalse();
    }

    [Fact]
    public async Task PendingReview_ShouldLeaveOutAnOfferWhoseProductCatalogDoesNotKnow()
    {
        var store = new FakeOfferReadStore { Pending = [new PendingOfferRow { OfferId = Guid.NewGuid(), ProductId = Guid.NewGuid() }] };

        Result<IReadOnlyCollection<PendingOfferResponse>> result = await new GetOffersPendingReviewQueryHandler(store, new FakeProductReader(Cement))
            .Handle(new GetOffersPendingReviewQuery(), CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task PendingReview_ShouldFail_WhenCatalogCannotBeRead()
    {
        var unreachable = Error.Failure("Catalog.Unreachable", "Catalog does not answer.");
        var store = new FakeOfferReadStore { Pending = [new PendingOfferRow { ProductId = Cement.Id }] };

        Result<IReadOnlyCollection<PendingOfferResponse>> result = await new GetOffersPendingReviewQueryHandler(store, new FakeProductReader(Cement) { Failure = unreachable })
            .Handle(new GetOffersPendingReviewQuery(), CancellationToken.None);

        result.Error.Should().Be(unreachable);
    }

    [Fact]
    public async Task Anomalies_ShouldNameTheProductsFromCatalog()
    {
        var store = new FakeOfferReadStore
        {
            Anomalies =
            [
                new PriceAnomalyRow { ProductId = Cement.Id, MerchantName = "Comptoir", Amount = 99m, Currency = "EUR" },
                new PriceAnomalyRow { ProductId = Guid.NewGuid(), MerchantName = "Ailleurs" }
            ]
        };

        Result<IReadOnlyCollection<PriceAnomalyResponse>> result = await new GetPriceAnomaliesQueryHandler(store, new FakeProductReader(Cement))
            .Handle(new GetPriceAnomaliesQuery(null, 10), CancellationToken.None);

        PriceAnomalyResponse anomaly = result.Value.Should().ContainSingle("the product of the second row is unknown").Subject;
        anomaly.ProductName.Should().Be("Ciment 35 kg");
        anomaly.Amount.Should().Be(99m);
    }

    [Fact]
    public async Task CollectionProfiles_ShouldNameTheTypologiesFromCatalog_AndPutTheUnknownOnesLast()
    {
        var gros = new ProductCategorySummary(Guid.NewGuid(), "Gros œuvre", null, 1);
        var outillage = new ProductCategorySummary(Guid.NewGuid(), "outillage", null, 1);
        var store = new FakeProfileReadStore(
            new CollectionProfileRow(Guid.NewGuid(), 0, false),
            new CollectionProfileRow(outillage.Id, 2, true),
            new CollectionProfileRow(gros.Id, 1, false));

        Result<IReadOnlyCollection<CollectionProfileSummaryResponse>> result =
            await new GetCollectionProfilesQueryHandler(store, new FakeProductCategoryReader(gros, outillage))
                .Handle(new GetCollectionProfilesQuery(), CancellationToken.None);

        result.Value.Select(s => s.CategoryName).Should().Equal("Gros œuvre", "outillage", null);
    }

    private sealed class FakeProfileReadStore(params CollectionProfileRow[] rows) : ICollectionProfileReadStore
    {
        public Task<IReadOnlyCollection<CollectionProfileRow>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CollectionProfileRow>>(rows);
    }

    private sealed class FakeOfferReadStore : IOfferReadStore
    {
        public PriceAnomalyRow[] Anomalies { get; init; } = [];

        public PendingOfferRow[] Pending { get; init; } = [];

        public Task<IReadOnlyCollection<PriceAnomalyRow>> GetPriceAnomaliesAsync(DateTime? sinceUtc, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<PriceAnomalyRow>>(Anomalies);

        public Task<IReadOnlyCollection<PendingOfferRow>> GetPendingReviewAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<PendingOfferRow>>(Pending);

        public Task<IReadOnlyCollection<PriceHistoryEntryResponse>> GetPriceHistoryAsync(Guid productId, DateTime? fromUtc, DateTime? toUtc, int limit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<MerchantPriceResponse>> GetPriceComparisonAsync(Guid productId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<ProductOfferRow>> GetProductOffersAsync(Guid productId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
