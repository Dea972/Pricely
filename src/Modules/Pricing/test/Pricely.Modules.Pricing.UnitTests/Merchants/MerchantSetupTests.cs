using FluentAssertions;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Application.Merchants.AddMerchantToProfiles;
using Pricely.Modules.Pricing.Application.Merchants.SuggestMerchant;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.UnitTests.Abstractions;

namespace Pricely.Modules.Pricing.UnitTests.Merchants;

public sealed class MerchantSetupTests
{
    private static readonly ProductCategorySummary Root = new(Guid.NewGuid(), "Gros œuvre", null, 1);
    private static readonly ProductCategorySummary Cement = new(Guid.NewGuid(), "Ciment", Root.Id, 2);

    private readonly FakeWeb _web = new();

    public MerchantSetupTests()
    {
        _web.Pages["https://comptoir.test/"] = new WebPage(new Uri("https://comptoir.test/"), string.Empty, new Uri("https://www.comptoir.test/"));
        _web.Pages["https://www.comptoir.test/"] = new WebPage(new Uri("https://www.comptoir.test/"), "Comptoir du Bâtiment, négoce de matériaux : ciment, mortier, parpaings.", null)
        {
            Title = "Comptoir du Bâtiment | Accueil",
            SearchTemplates = ["https://www.comptoir.test/recherche?q={query}", "https://ailleurs.test/s?q={query}"]
        };
        _web.Pages["https://www.comptoir.test/recherche?q=ciment"] = new WebPage(
            new Uri("https://www.comptoir.test/recherche?q=ciment"),
            "12 résultats pour ciment. " + new string('x', 300),
            null);
    }

    [Fact]
    public async Task Suggest_ShouldKeepOnlyWhatTheSiteSupports_AndTestTheSearchPage()
    {
        var model = new FakeModel($$"""
            Voici ma proposition :
            ```json
            {"name":"Comptoir du Bâtiment","searchTemplate":"https://evil.test/?q={query}","testQuery":"ciment",
             "categoryIds":["{{Cement.Id}}","{{Guid.NewGuid()}}","pas-un-guid"],"categoryReasons":{"{{Cement.Id}}":"Vend du ciment en sac."},
             "rationale":"Négoce de matériaux ; page de recherche déclarée par le site."}
            ```
            """);

        Result<MerchantSuggestionResponse> result = await Handler(model).Handle(new SuggestMerchantCommand("comptoir.test", null), CancellationToken.None);

        MerchantSuggestionResponse suggestion = result.Value;
        suggestion.Website.Should().Be(new Uri("https://www.comptoir.test/"), "the home page after its redirect");
        suggestion.SearchTemplate.Should().Be("https://www.comptoir.test/recherche?q={query}", "the proposed page is not on the site");
        suggestion.SearchCandidates.Should().Equal("https://www.comptoir.test/recherche?q={query}");
        suggestion.SearchCheck.Should().BeEquivalentTo(new { Query = "ciment", IsReachable = true, ShowsQuery = true });
        suggestion.Categories.Should().ContainSingle().Which.Should().Be(new SuggestedCategoryResponse(Cement.Id, "Gros œuvre › Ciment", "Vend du ciment en sac."));
        suggestion.IsAiAnswer.Should().BeTrue();
        model.LastMessage.Should().Contain(Cement.Id.ToString()).And.Contain("Gros œuvre › Ciment").And.Contain("recherche?q={query}");
    }

    [Fact]
    public async Task Suggest_WithoutTheModel_ShouldStillProposeWhatTheSiteDeclares()
    {
        Result<MerchantSuggestionResponse> result = await Handler(new FakeModel(null)).Handle(new SuggestMerchantCommand("https://www.comptoir.test", null), CancellationToken.None);

        result.Value.Should().BeEquivalentTo(new
        {
            Name = "Comptoir du Bâtiment | Accueil",
            SearchTemplate = "https://www.comptoir.test/recherche?q={query}",
            IsAiAnswer = false
        });
        result.Value.Categories.Should().BeEmpty();
        result.Value.SearchCheck!.IsReachable.Should().BeFalse("the default test query has no result page here");
    }

    [Fact]
    public async Task Suggest_ShouldRefuseAnAddressThatIsNotAWebSite()
    {
        Result<MerchantSuggestionResponse> result = await Handler(new FakeModel(null)).Handle(new SuggestMerchantCommand("ftp://files", null), CancellationToken.None);

        result.Error.Code.Should().Be("Merchants.InvalidWebsite");
    }

    [Fact]
    public async Task AddToProfiles_ShouldKeepTheInheritedMerchants_ThenAddTheNewOne()
    {
        Merchant inherited = Merchant.Register("Négoce", "https://negoce.test").Value;
        Merchant added = Merchant.Register("Comptoir", "https://comptoir.test").Value;
        var profiles = new FakeProfiles(CollectionProfile.Define(Root.Id, [inherited.Id], "Prix TTC.").Value);

        var handler = new AddMerchantToProfilesCommandHandler(new FakeMerchants(inherited, added), new FakeProductCategoryReader(Root, Cement), profiles, new NoUnitOfWork());
        Result result = await handler.Handle(new AddMerchantToProfilesCommand(added.Id, [Cement.Id, Root.Id]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        profiles.Of(Cement.Id)!.PreferredMerchants.Select(m => m.MerchantId).Should().Equal(inherited.Id, added.Id);
        profiles.Of(Cement.Id)!.Instructions.Should().BeNull("the instructions of the parent are still inherited");
        profiles.Of(Root.Id)!.PreferredMerchants.Select(m => m.MerchantId).Should().Equal(inherited.Id, added.Id);
        profiles.Of(Root.Id)!.Instructions.Should().Be("Prix TTC.");
    }

    [Fact]
    public async Task AddToProfiles_ShouldRefuseATypologyCatalogDoesNotKnow()
    {
        Merchant added = Merchant.Register("Comptoir", "https://comptoir.test").Value;
        var profiles = new FakeProfiles();
        var unknown = Guid.NewGuid();

        var handler = new AddMerchantToProfilesCommandHandler(new FakeMerchants(added), new FakeProductCategoryReader(Root, Cement), profiles, new NoUnitOfWork());
        Result result = await handler.Handle(new AddMerchantToProfilesCommand(added.Id, [unknown]), CancellationToken.None);

        result.Error.Code.Should().Be("Catalog.CategoryNotFound");
        profiles.Of(unknown).Should().BeNull();
    }

    [Fact]
    public async Task AddToProfiles_ShouldFail_WhenCatalogCannotBeRead()
    {
        Merchant added = Merchant.Register("Comptoir", "https://comptoir.test").Value;
        var unreachable = Error.Failure("Catalog.Unreachable", "Catalog does not answer.");
        var profiles = new FakeProfiles();

        var handler = new AddMerchantToProfilesCommandHandler(new FakeMerchants(added), new FakeProductCategoryReader(Root) { Failure = unreachable }, profiles, new NoUnitOfWork());
        Result result = await handler.Handle(new AddMerchantToProfilesCommand(added.Id, [Root.Id]), CancellationToken.None);

        result.Error.Should().Be(unreachable);
        profiles.Of(Root.Id).Should().BeNull();
    }

    [Fact]
    public async Task Suggest_ShouldFail_WhenCatalogCannotBeRead()
    {
        var unreachable = Error.Failure("Catalog.Unreachable", "Catalog does not answer.");
        var handler = new SuggestMerchantCommandHandler(_web, new FakeGateway(new FakeModel(null)), new FakeProductCategoryReader(Root) { Failure = unreachable });

        Result<MerchantSuggestionResponse> result = await handler.Handle(new SuggestMerchantCommand("comptoir.test", null), CancellationToken.None);

        result.Error.Should().Be(unreachable);
    }

    private SuggestMerchantCommandHandler Handler(FakeModel model) =>
        new(_web, new FakeGateway(model), new FakeProductCategoryReader(Root, Cement));

    private sealed class FakeWeb : IWebPageReader
    {
        public Dictionary<string, WebPage> Pages { get; } = new(StringComparer.Ordinal);

        public Task<Result<WebPage>> ReadAsync(Uri address, CancellationToken cancellationToken = default) =>
            Task.FromResult(Pages.TryGetValue(address.AbsoluteUri, out WebPage? page)
                ? Result.Success(page)
                : Result.Failure<WebPage>(WebErrors.Unreadable(address, "HTTP 404")));
    }

    /// <summary>Answers with the given content, or fails like a provider without a key.</summary>
    private sealed class FakeModel(string? content) : LlmServiceBase(new NoEventBus(), new Clock())
    {
        public string? LastMessage { get; private set; }

        public override string ProviderName => "Fake";

        protected override string DefaultModel => "fake";

        protected override Task<LlmResponse> SendCoreAsync(LlmRequest request, string model, CancellationToken cancellationToken)
        {
            LastMessage = request.Prompt.Message;

            return content is null
                ? throw new HttpRequestException("401 Unauthorized")
                : Task.FromResult(new LlmResponse(content, LlmTokenUsage.None, model));
        }
    }

    private sealed class FakeGateway(LlmServiceBase service) : ILlmGateway
    {
        public Task<LlmClient> ForFeatureAsync(AiFeature feature, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LlmClient(service, feature, new LlmTarget("fake")));
    }

    private sealed class FakeProfiles(params CollectionProfile[] profiles) : ICollectionProfileRepository
    {
        private readonly List<CollectionProfile> _profiles = [.. profiles];

        public CollectionProfile? Of(Guid categoryId) => _profiles.Find(p => p.ProductCategoryId == categoryId);

        public Task<CollectionProfile?> GetByCategoryAsync(Guid productCategoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Of(productCategoryId));

        public Task<IReadOnlyCollection<CollectionProfile>> GetByCategoriesAsync(IReadOnlyCollection<Guid> productCategoryIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CollectionProfile>>([.. _profiles.Where(p => productCategoryIds.Contains(p.ProductCategoryId))]);

        public void Insert(CollectionProfile profile) => _profiles.Add(profile);
    }

    private sealed class FakeMerchants(params Merchant[] merchants) : IMerchantRepository
    {
        public Task<Merchant?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Find(merchants, m => m.Id == id));

        public Task<IReadOnlyCollection<Merchant>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Merchant>>([.. merchants.Where(m => ids.Contains(m.Id))]);

        public void Insert(Merchant merchant) => throw new NotSupportedException();
    }

    private sealed class NoUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
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
