using System.Runtime.CompilerServices;
using FluentAssertions;
using MediatR;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;
using Pricely.Modules.Catalog.Contracts.Products;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Application.Merchants.AddMerchantToProfiles;
using Pricely.Modules.Pricing.Application.Merchants.FindOrRegisterMerchant;
using Pricely.Modules.Pricing.Application.Offers.AddWebOffer;
using Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;
using Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;
using Pricely.Modules.Pricing.Application.PageSnapshots;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.Domain.PageSnapshots;
using Pricely.Modules.Pricing.UnitTests.Abstractions;

namespace Pricely.Modules.Pricing.UnitTests.WebResearch;

/// <summary>The writes of a web research batch (ADR-028): find or add a merchant, add an offer read again at approval.</summary>
public sealed class WebBatchTests
{
    private static readonly ProductCategorySummary Cement = new(Guid.NewGuid(), "Ciment", null, 1);
    private static readonly ProductSummary Product = new(Guid.NewGuid(), "Ciment 35 kg", "Cimenterie", "3760000100014", "CR-35", Cement.Id);
    private static readonly Uri Page = new("https://www.comptoir.test/p/ciment-35");

    private readonly InMemoryMerchants _merchants = new(Merchant.Register("Comptoir", "https://www.comptoir.test/").Value);
    private readonly FakeSite _site = new();
    private readonly InMemorySnapshots _snapshots = new();
    private readonly RecordingSender _sender = new();

    [Fact]
    public async Task Merchant_ShouldBeFoundBySite_FromAnyPageOfIt()
    {
        Result<FoundMerchantResponse> result = await new FindOrRegisterMerchantCommandHandler(_merchants, new NoUnitOfWork())
            .Handle(new FindOrRegisterMerchantCommand("Comptoir du Bâtiment", new Uri("https://comptoir.test/p/vis")), CancellationToken.None);

        result.Value.Should().BeEquivalentTo(new { MerchantId = _merchants.Items[0].Id, IsNew = false });
    }

    [Fact]
    public async Task Merchant_ShouldBeRegisteredOnTheRootOfItsSite_UnlessItsNameIsTaken()
    {
        var handler = new FindOrRegisterMerchantCommandHandler(_merchants, new NoUnitOfWork());

        Result<FoundMerchantResponse> added = await handler.Handle(new FindOrRegisterMerchantCommand("Négoce Atlantique", new Uri("https://negoce.test/fr/p/1?x=2")), CancellationToken.None);
        Result<FoundMerchantResponse> taken = await handler.Handle(new FindOrRegisterMerchantCommand("comptoir", new Uri("https://autre.test/")), CancellationToken.None);

        added.Value.Should().BeEquivalentTo(new { IsNew = true, Website = new Uri("https://negoce.test/") });
        _merchants.Items.Should().HaveCount(2);
        taken.Error.Code.Should().Be("Merchants.NameUsedByAnotherSite");
    }

    [Fact]
    public async Task Offer_ShouldReadThePageAgain_JoinTheProfile_ThenGoThroughTheSubmittedOfferRules()
    {
        _site.Pages[Page.AbsoluteUri] = new WebPage(Page, "Ciment CEM II 35 kg — 9,40 € TTC — EAN 3760000100014", null)
        {
            Products = [new StructuredProduct("Ciment CEM II 35 kg", "3760000100014", "Cimenterie", "C35", [new StructuredOffer(9.4m, "EUR", "InStock", null)])]
        };

        Result<AddedWebOfferResponse> result = await Handler().Handle(
            new AddWebOfferCommand("3760000100014", null, null, Page, 9.4m, "eur"),
            CancellationToken.None);

        result.Value.Should().BeEquivalentTo(new { ProductId = Product.Id, MerchantName = "Comptoir", Outcome = "NewOfferConfirmed", AddedToProfile = true, Currency = "EUR" });
        _site.Requested.Should().ContainSingle().Which.Should().Be(Page);
        _sender.Sent.OfType<AddMerchantToProfilesCommand>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { MerchantId = _merchants.Items[0].Id, ProductCategoryIds = new[] { Cement.Id } });
        SubmitCollectedOfferCommand submitted = _sender.Sent.OfType<SubmitCollectedOfferCommand>().Should().ContainSingle().Subject;
        submitted.ProductId.Should().Be(Product.Id);
        submitted.Offer.Should().BeEquivalentTo(new
        {
            SnapshotId = _snapshots.Items.Single().Id,
            Price = 9.4m,
            Availability = Availability.InStock,
            MerchantProductReference = "C35",
            ObservedGtin = "3760000100014"
        });
    }

    [Fact]
    public async Task Offer_ShouldNeedAKnownProduct_AndAFollowedMerchant_BeforeAnyRead()
    {
        Result<AddedWebOfferResponse> unknownProduct = await Handler().Handle(
            new AddWebOfferCommand(null, "Autre", "X-1", Page, 9.4m, "EUR"), CancellationToken.None);
        Result<AddedWebOfferResponse> unknownMerchant = await Handler().Handle(
            new AddWebOfferCommand(Product.Gtin, null, null, new Uri("https://ailleurs.test/p/1"), 9.4m, "EUR"), CancellationToken.None);

        unknownProduct.Error.Code.Should().Be("Catalog.ProductNotFoundByKey");
        unknownMerchant.Error.Code.Should().Be("Merchants.NoMerchantForSite");
        _site.Requested.Should().BeEmpty();
        _sender.Sent.Should().BeEmpty();
    }

    private AddWebOfferCommandHandler Handler()
    {
        var products = new FakeProductReader(Product);

        return new AddWebOfferCommandHandler(
            products,
            _merchants,
            new MerchantPageReader(_merchants, _site, _snapshots, new FixedClock(), new NoUnitOfWork()),
            new OfferCollectionPlanner(products, new FakeProductCategoryReader(Cement), new NoProfiles(), _merchants),
            _sender);
    }

    private sealed class FakeSite : IWebPageReader
    {
        public Dictionary<string, WebPage> Pages { get; } = new(StringComparer.Ordinal);

        public List<Uri> Requested { get; } = [];

        public Task<Result<WebPage>> ReadAsync(Uri address, CancellationToken cancellationToken = default)
        {
            Requested.Add(address);

            return Task.FromResult(Pages.TryGetValue(address.AbsoluteUri, out WebPage? page)
                ? Result.Success(page)
                : Result.Failure<WebPage>(WebErrors.Unreadable(address, "HTTP 404")));
        }
    }

    private sealed class InMemoryMerchants(params Merchant[] merchants) : IMerchantRepository
    {
        public List<Merchant> Items { get; } = [.. merchants];

        public Task<Merchant?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Find(m => m.Id == id));

        public Task<IReadOnlyCollection<Merchant>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Merchant>>([.. Items.Where(m => ids.Contains(m.Id))]);

        public Task<IReadOnlyCollection<Merchant>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Merchant>>([.. Items]);

        public void Insert(Merchant merchant) => Items.Add(merchant);
    }

    private sealed class InMemorySnapshots : IPageSnapshotRepository
    {
        public List<PageSnapshot> Items { get; } = [];

        public Task<PageSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Find(s => s.Id == id));

        public void Insert(PageSnapshot snapshot) => Items.Add(snapshot);
    }

    private sealed class NoProfiles : ICollectionProfileRepository
    {
        public Task<CollectionProfile?> GetByCategoryAsync(Guid productCategoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CollectionProfile?>(null);

        public Task<IReadOnlyCollection<CollectionProfile>> GetByCategoriesAsync(
            IReadOnlyCollection<Guid> productCategoryIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CollectionProfile>>([]);

        public void Insert(CollectionProfile profile) => throw new NotSupportedException();
    }

    private sealed class NoUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>Records the composed commands; adding to a profile succeeds, a submitted offer is confirmed.</summary>
    private sealed class RecordingSender : ISender
    {
        public List<object> Sent { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Sent.Add(request);
            object response = request switch
            {
                AddMerchantToProfilesCommand => Result.Success(),
                SubmitCollectedOfferCommand => Result.Success(new SubmittedOfferResponse("NewOfferConfirmed", false)),
                _ => throw new NotSupportedException(request.GetType().Name)
            };

            return Task.FromResult((TResponse)response);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
