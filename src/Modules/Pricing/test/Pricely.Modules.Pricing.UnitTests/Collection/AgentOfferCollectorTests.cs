using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;
using Pricely.Modules.Pricing.Application.PageSnapshots;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.PageSnapshots;
using Pricely.Modules.Pricing.Infrastructure.Collection;

namespace Pricely.Modules.Pricing.UnitTests.Collection;

public sealed class AgentOfferCollectorTests
{
    private const string ProductPage = "https://www.comptoir.test/p/ciment-cem-ii";

    private readonly Merchant _comptoir = Merchant.Register("Comptoir", "https://www.comptoir.test", "https://www.comptoir.test/recherche?q={query}").Value;
    private readonly Merchant _outsidePlan = Merchant.Register("Hors plan", "https://www.autre.test").Value;
    private readonly FakeSite _site = new();

    [Fact]
    public async Task Collect_ShouldKeepTheOffer_ReadOnAFetchedPage()
    {
        using var model = new ScriptedModel(
            _ => Call("fetch_merchant_page", new() { ["merchantId"] = _comptoir.Id, ["searchText"] = "ciment CEM II" }),
            _ => Call("fetch_merchant_page", new() { ["merchantId"] = _comptoir.Id, ["address"] = ProductPage }),
            history => Call("submit_offer", new() { ["offer"] = Offer(LastSnapshotId(history), 9.40m) }),
            _ => Answer("Comptoir : 9,40 €."));

        Result<IReadOnlyCollection<CollectedOffer>> result = await CollectAsync(model);

        CollectedOffer offer = result.Value.Should().ContainSingle().Subject;
        offer.MerchantId.Should().Be(_comptoir.Id);
        offer.ProductPage.Should().Be(new Uri(ProductPage), "the page is the snapshot's, not the model's");
        offer.PriceAmount.Should().Be(9.40m);
        offer.Availability.Should().Be(Domain.Offers.Availability.InStock);
        offer.Evidence.ObservedTitle.Should().Be("Ciment CEM II 35 kg");
        offer.Evidence.AiRationale.Should().Be("Même GTIN.");
        offer.Evidence.ObservedGtin.Should().BeNull("the page does not show it: a GTIN the collector did not read would confirm the offer");
        _site.Requested.Should().Equal(new Uri("https://www.comptoir.test/recherche?q=ciment%20CEM%20II"), new Uri(ProductPage));
    }

    [Fact]
    public async Task Collect_ShouldKeepAnIdentifier_ThatThePageShows()
    {
        _site.Text = "Ciment CEM II 35 kg\nEAN : 3456789012344\n9,40 € TTC";
        using var model = new ScriptedModel(
            _ => Call("fetch_merchant_page", new() { ["merchantId"] = _comptoir.Id, ["address"] = ProductPage }),
            history => Call("submit_offer", new() { ["offer"] = Offer(LastSnapshotId(history), 9.40m) }),
            _ => Answer("Comptoir : 9,40 €."));

        Result<IReadOnlyCollection<CollectedOffer>> result = await CollectAsync(model);

        result.Value.Should().ContainSingle().Which.Evidence.ObservedGtin.Should().Be("3456789012344");
    }

    [Fact]
    public async Task Collect_ShouldRefuseAnOffer_ReadOnTheSearchPage()
    {
        using var model = new ScriptedModel(
            _ => Call("fetch_merchant_page", new() { ["merchantId"] = _comptoir.Id, ["searchText"] = "ciment" }),
            history => Call("submit_offer", new() { ["offer"] = Offer(LastSnapshotId(history), 9.40m) }),
            history => Answer(LastResult(history)));

        Result<IReadOnlyCollection<CollectedOffer>> result = await CollectAsync(model);

        result.Value.Should().BeEmpty();
        model.LastAnswer.Should().Contain("PageSnapshots.SearchPage");
    }

    [Fact]
    public async Task Collect_ShouldRefuseAPrice_ThatThePageDoesNotShow()
    {
        using var model = new ScriptedModel(
            _ => Call("fetch_merchant_page", new() { ["merchantId"] = _comptoir.Id, ["address"] = ProductPage }),
            history => Call("submit_offer", new() { ["offer"] = Offer(LastSnapshotId(history), 8.90m) }),
            history => Answer(LastResult(history)));

        Result<IReadOnlyCollection<CollectedOffer>> result = await CollectAsync(model);

        result.Value.Should().BeEmpty();
        model.LastAnswer.Should().Contain("PageSnapshots.PriceNotShown");
    }

    [Fact]
    public async Task Collect_ShouldNeverReadOutsideTheMerchantsOfThePlan()
    {
        using var model = new ScriptedModel(
            _ => Call("fetch_merchant_page", new() { ["merchantId"] = _outsidePlan.Id, ["address"] = "https://www.autre.test/p" }),
            history => Call("fetch_merchant_page", new() { ["merchantId"] = _comptoir.Id, ["address"] = "http://169.254.169.254/latest/meta-data" }),
            history => Answer(LastResult(history)));

        Result<IReadOnlyCollection<CollectedOffer>> result = await CollectAsync(model);

        result.Value.Should().BeEmpty();
        model.LastAnswer.Should().Contain("Merchants.AddressOutsideMerchant");
        _site.Requested.Should().BeEmpty();
    }

    [Fact]
    public async Task Collect_ShouldRefuseARedirect_ToAnotherSite()
    {
        _site.RedirectTo = new Uri("https://www.autre.test/p");
        using var model = new ScriptedModel(
            _ => Call("fetch_merchant_page", new() { ["merchantId"] = _comptoir.Id, ["address"] = ProductPage }),
            history => Answer(LastResult(history)));

        await CollectAsync(model);

        model.LastAnswer.Should().Contain("Merchants.AddressOutsideMerchant");
        _site.Requested.Should().ContainSingle();
    }

    [Fact]
    public async Task Collect_ShouldRefuseASnapshot_NotFetchedDuringTheRun()
    {
        using var model = new ScriptedModel(
            _ => Call("submit_offer", new() { ["offer"] = Offer(Guid.NewGuid(), 9.40m) }),
            history => Answer(LastResult(history)));

        Result<IReadOnlyCollection<CollectedOffer>> result = await CollectAsync(model);

        result.Value.Should().BeEmpty();
        model.LastAnswer.Should().Contain("PageSnapshots.NotFound");
    }

    private async Task<Result<IReadOnlyCollection<CollectedOffer>>> CollectAsync(ScriptedModel model)
    {
        var merchants = new InMemoryMerchants(_comptoir, _outsidePlan);
        var snapshots = new InMemorySnapshots();
        var clock = new FixedClock();
        var reader = new MerchantPageReader(merchants, _site, snapshots, clock, new NoUnitOfWork());
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        var collector = new AgentOfferCollector(
            new FakeGateway(new FakeLlmService(model, clock)),
            reader,
            new SubmittedOfferVerifier(snapshots),
            NullLoggerFactory.Instance,
            services);

        return await collector.CollectAsync(new OfferCollectionRequest(
            new ProductToCollect(Guid.NewGuid(), "Ciment CEM II", "CR", "03456789012344", null),
            [new MerchantToSearch(_comptoir.Id, _comptoir.Name, _comptoir.Website, 1, _comptoir.SearchTemplate)],
            string.Empty));
    }

    private static JsonElement Offer(Guid snapshotId, decimal price) =>
        JsonSerializer.SerializeToElement(new
        {
            snapshotId,
            price,
            currency = "EUR",
            availability = "InStock",
            observedTitle = "Ciment CEM II 35 kg",
            observedGtin = "3456789012344",
            similarity = 0.95m,
            rationale = "Même GTIN."
        });

    private static ChatResponse Call(string name, Dictionary<string, object?> arguments) =>
        new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent(Guid.NewGuid().ToString(), name, arguments)]));

    private static ChatResponse Answer(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    private static string LastResult(IReadOnlyList<ChatMessage> history) =>
        JsonSerializer.Serialize(history[^1].Contents.OfType<FunctionResultContent>().Single().Result);

    private static Guid LastSnapshotId(IReadOnlyList<ChatMessage> history)
    {
        using var result = JsonDocument.Parse(LastResult(history));

        return result.RootElement.EnumerateObject()
            .Single(p => p.Name.Equals("snapshotId", StringComparison.OrdinalIgnoreCase))
            .Value.GetGuid();
    }

    /// <summary>A model that plays its steps in order, each reading the conversation so far (tool results included).</summary>
    private sealed class ScriptedModel(params Func<IReadOnlyList<ChatMessage>, ChatResponse>[] steps) : IChatClient
    {
        private readonly Queue<Func<IReadOnlyList<ChatMessage>, ChatResponse>> _steps = new(steps);

        public string LastAnswer { get; private set; } = string.Empty;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            ChatResponse response = _steps.Dequeue()([.. messages]);
            LastAnswer = response.Text;

            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);

            foreach (ChatResponseUpdate update in response.ToChatResponseUpdates())
            {
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class FakeSite : IWebPageReader
    {
        public List<Uri> Requested { get; } = [];

        public Uri? RedirectTo { get; set; }

        public string Text { get; set; } = "Ciment CEM II 35 kg\n9,40 € TTC\nEn stock";

        public Task<Result<WebPage>> ReadAsync(Uri address, CancellationToken cancellationToken = default)
        {
            Requested.Add(address);

            return Task.FromResult(Result.Success(new WebPage(address, Text, RedirectTo)));
        }
    }

    private sealed class InMemoryMerchants(params Merchant[] merchants) : IMerchantRepository
    {
        public Task<Merchant?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Find(merchants, m => m.Id == id));

        public Task<IReadOnlyCollection<Merchant>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Merchant>>([.. merchants.Where(m => ids.Contains(m.Id))]);

        public void Insert(Merchant merchant) => throw new NotSupportedException();
    }

    private sealed class InMemorySnapshots : IPageSnapshotRepository
    {
        private readonly List<PageSnapshot> _snapshots = [];

        public Task<PageSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshots.Find(s => s.Id == id));

        public void Insert(PageSnapshot snapshot) => _snapshots.Add(snapshot);
    }

    private sealed class NoUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc);
    }

    private sealed class NoEventBus : IEventBus
    {
        public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
            where T : IIntegrationEvent => Task.CompletedTask;
    }

    private sealed class FakeLlmService(IChatClient chatClient, IDateTimeProvider clock) : LlmServiceBase(new NoEventBus(), clock)
    {
        public override string ProviderName => "Fake";

        protected override string DefaultModel => "fake-model";

        protected override Task<LlmResponse> SendCoreAsync(LlmRequest request, string model, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        protected override IChatClient CreateChatClientCore(string model) => chatClient;
    }

    private sealed class FakeGateway(LlmServiceBase service) : ILlmGateway
    {
        public Task<LlmClient> ForFeatureAsync(AiFeature feature, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LlmClient(service, feature, new LlmTarget("fake")));
    }
}
