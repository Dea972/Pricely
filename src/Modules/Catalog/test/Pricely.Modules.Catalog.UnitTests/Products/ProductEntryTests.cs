using FluentAssertions;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Products.SuggestProduct;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.UnitTests.Products;

public sealed class ProductEntryTests
{
    private static readonly Uri Page = new("https://fabricant.test/ciment-cem-ii");
    private static readonly ProductCategory Cement = ProductCategory.CreateRoot("Ciment").Value;

    private const string PageText = """
        Ciment CEM II/B-LL 32,5 R — sac de 35 kg
        Marque : Cimenterie du Rhône · Réf. cr 325r 35
        EAN 3760000100014
        """;

    [Fact]
    public async Task Suggest_ShouldKeepTheIdentifiersThePageShows_AndPointOutTheExistingProduct()
    {
        Product existing = Product.Register(new ProductDescription("Ciment CEM II 35 kg", "CR", Gtin.Create("3760000100014").Value, null), Cement.Id).Value;
        var model = new FakeModel($$"""{"name":"Ciment CEM II/B-LL 32,5 R — sac 35 kg","brand":"Cimenterie du Rhône","gtin":"03760000100014","manufacturerReference":"CR-325R-35","categoryId":"{{Cement.Id}}","packaging":"sac 35 kg","rationale":"GTIN et référence affichés."}""");

        Result<ProductSuggestionResponse> result = await Handler(model, existing).Handle(new SuggestProductCommand(Page), CancellationToken.None);

        result.Value.Should().BeEquivalentTo(new
        {
            Name = "Ciment CEM II/B-LL 32,5 R — sac 35 kg",
            Brand = "Cimenterie du Rhône",
            Gtin = "03760000100014",
            ManufacturerReference = "CR-325R-35",
            ProductCategoryId = (Guid?)Cement.Id,
            ExistingProductId = (Guid?)existing.Id,
            IsAiAnswer = true
        });
    }

    [Fact]
    public async Task Suggest_ShouldDropAnInventedGtin_AReferenceNotShown_AndAnUnknownTypology()
    {
        var model = new FakeModel($$"""{"name":"Ciment","brand":"CR","gtin":"3760000100021","manufacturerReference":"XX-999","categoryId":"{{Guid.NewGuid()}}"}""");

        Result<ProductSuggestionResponse> result = await Handler(model, null).Handle(new SuggestProductCommand(Page), CancellationToken.None);

        result.Value.Gtin.Should().BeNull("the page shows another GTIN");
        result.Value.ManufacturerReference.Should().BeNull();
        result.Value.ProductCategoryId.Should().BeNull();
        result.Value.ExistingProductId.Should().BeNull();
    }

    [Fact]
    public async Task Suggest_WithoutTheModel_ShouldProposeTheTitleOfThePage()
    {
        Result<ProductSuggestionResponse> result = await Handler(new FakeModel(null), null).Handle(new SuggestProductCommand(Page), CancellationToken.None);

        result.Value.Should().BeEquivalentTo(new { Name = "Ciment CEM II — Cimenterie du Rhône", IsAiAnswer = false, Gtin = (string?)null });
    }

    private static SuggestProductCommandHandler Handler(FakeModel model, Product? existing) =>
        new(new FakeWeb(), new FakeGateway(model), new FakeCategories(), new FakeProducts(existing));

    private sealed class FakeWeb : IWebPageReader
    {
        public Task<Result<WebPage>> ReadAsync(Uri address, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(new WebPage(address, PageText, null) { Title = "Ciment CEM II — Cimenterie du Rhône" }));
    }

    private sealed class FakeModel(string? content) : LlmServiceBase(new NoEventBus(), new Clock())
    {
        public override string ProviderName => "Fake";

        protected override string DefaultModel => "fake";

        protected override Task<LlmResponse> SendCoreAsync(LlmRequest request, string model, CancellationToken cancellationToken) =>
            content is null
                ? throw new HttpRequestException("401 Unauthorized")
                : Task.FromResult(new LlmResponse(content, LlmTokenUsage.None, model));
    }

    private sealed class FakeGateway(LlmServiceBase service) : ILlmGateway
    {
        public Task<LlmClient> ForFeatureAsync(AiFeature feature, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LlmClient(service, feature, new LlmTarget("fake")));
    }

    private sealed class FakeCategories : IProductCategoryRepository
    {
        public Task<ProductCategory?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == Cement.Id ? Cement : null);

        public Task<IReadOnlyCollection<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ProductCategory>>([Cement]);

        public void Insert(ProductCategory category) => throw new NotSupportedException();
    }

    private sealed class FakeProducts(Product? existing) : IProductRepository
    {
        public Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Product?>(null);

        public Task<bool> ExistsWithGtinAsync(Gtin gtin, Guid? excludedProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult(existing is not null);

        public Task<Product?> FindByGtinAsync(Gtin gtin, CancellationToken cancellationToken = default) =>
            Task.FromResult(existing?.Gtin == gtin ? existing : null);

        public void Insert(Product product) => throw new NotSupportedException();
    }

    private sealed class NoEventBus : IEventBus
    {
        public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
            where T : IIntegrationEvent => Task.CompletedTask;
    }

    private sealed class Clock : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc);
    }
}
