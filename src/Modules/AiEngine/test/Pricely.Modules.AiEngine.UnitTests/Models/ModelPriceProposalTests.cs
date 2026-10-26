using System.Text.Json;
using FluentAssertions;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Prices;
using Pricely.Modules.AiEngine.Application.Models.ProposeModelPrices;
using Pricely.Modules.AiEngine.Domain.Models;
using Pricely.Modules.AiEngine.Infrastructure.Prices;

namespace Pricely.Modules.AiEngine.UnitTests.Models;

public sealed class ModelPriceProposalTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void LiteLlm_ShouldGiveThePricesPerMillion_OfTheModelOfTheProvider()
    {
        using var list = JsonDocument.Parse("""
            {"claude-sonnet-5-5":{"input_cost_per_token":2e-06,"output_cost_per_token":1e-05,"cache_read_input_token_cost":1e-07,"cache_creation_input_token_cost":2.5e-06,"litellm_provider":"anthropic"},
             "claude-other":{"input_cost_per_token":1e-06,"output_cost_per_token":2e-06,"litellm_provider":"bedrock"},
             "anthropic/claude-prefixed":{"input_cost_per_token":1e-06,"output_cost_per_token":5e-06}}
            """);

        LiteLlmPriceList.Find(list.RootElement, new PublishedPriceQuery(Provider.Anthropic, "claude-sonnet-5-5"))
            .Should().Be(new PublishedPrice(Provider.Anthropic, "claude-sonnet-5-5", 2m, 0.1m, 2.5m, 10m));
        LiteLlmPriceList.Find(list.RootElement, new PublishedPriceQuery(Provider.Anthropic, "claude-other")).Should().BeNull("another provider sells it");
        LiteLlmPriceList.Find(list.RootElement, new PublishedPriceQuery(Provider.Anthropic, "claude-prefixed"))!.CachedInputPerMillion.Should().BeNull();
    }

    [Fact]
    public async Task Proposals_ShouldComeFromLiteLlm_ElseFromThePricingPage_OnlyWhatThePageShows()
    {
        Model known = Model.Create(Provider.Anthropic, "claude-sonnet-5-5", "Sonnet", Now).Value;
        known.SetPrice(new DateOnly(2026, 1, 1), new PriceValues(3m, 0.3m, 3.75m, 15m), PriceSource.Manual, Now);
        Model read = Model.Create(Provider.Anthropic, "claude-fable-5-1", "Fable", Now).Value;
        Model invented = Model.Create(Provider.Anthropic, "claude-unknown", "Unknown", Now).Value;

        var model = new FakeModel("""
            {"prices":[{"model":"claude-fable-5-1","input":5,"cachedInput":0.42,"cacheWrite":6.25,"output":25},
                       {"model":"claude-unknown","input":5,"output":0}]}
            """);
        var handler = new ProposeModelPricesCommandHandler(
            new FakeModels(known, read, invented),
            new FakeList(new PublishedPrice(Provider.Anthropic, "claude-sonnet-5-5", 2m, 0.1m, 2.5m, 10m)),
            new FakeWeb("Claude Fable 5.1 | $5 / MTok | $6.25 / MTok | $25 / MTok\nAutre ligne sans prix"),
            new FakeGateway(model),
            new ModelPriceOptions { PricingPages = { ["Anthropic"] = new Uri("https://pricing.test/") } },
            new Clock());

        IReadOnlyCollection<ModelPriceProposalResponse> proposals = (await handler.Handle(new ProposeModelPricesCommand(), CancellationToken.None)).Value;

        ModelPriceProposalResponse sonnet = proposals.Single(p => p.ProviderModelId == "claude-sonnet-5-5");
        sonnet.Should().BeEquivalentTo(new { Source = "LiteLlm", IsChanged = true, Notes = Array.Empty<string>() });
        sonnet.Current!.InputPerMillion.Should().Be(3m);
        sonnet.Proposed.Should().Be(new PriceResponse(2m, 0.1m, 2.5m, 10m));

        ModelPriceProposalResponse fable = proposals.Single(p => p.ProviderModelId == "claude-fable-5-1");
        fable.Source.Should().Be("AiReading");
        fable.Proposed.Should().Be(new PriceResponse(5m, 5m, 6.25m, 25m), "the cache read price is not on the page: the input price is proposed");
        fable.Notes.Should().Equal("ToCheck", "CachePricesMissing");

        ModelPriceProposalResponse unknown = proposals.Single(p => p.ProviderModelId == "claude-unknown");
        unknown.Should().BeEquivalentTo(new { Proposed = (PriceResponse?)null, IsChanged = false }, "a free model is never taken from the page, even if $5 is on it");
        unknown.Notes.Should().Equal("NotFound");
        model.LastMessage.Should().Contain("claude-fable-5-1").And.NotContain("claude-sonnet-5-5", "LiteLLM knows it").And.NotContain("Autre ligne");
    }

    private sealed class FakeModels(params Model[] models) : IModelRepository
    {
        public Task<Model?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Array.Find(models, m => m.Id == id));

        public Task<Model?> FindAsync(Provider provider, string providerModelId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> ExistsAsync(Provider provider, string providerModelId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<Model>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Model>>(models);

        public void Insert(Model model) => throw new NotSupportedException();
    }

    private sealed class FakeList(params PublishedPrice[] prices) : IPublishedPriceList
    {
        public Task<Result<IReadOnlyCollection<PublishedPrice>>> FindAsync(IReadOnlyCollection<PublishedPriceQuery> models, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success<IReadOnlyCollection<PublishedPrice>>(prices));
    }

    private sealed class FakeWeb(string text) : IWebPageReader
    {
        public Task<Result<WebPage>> ReadAsync(Uri address, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(new WebPage(address, text, null)));
    }

    private sealed class FakeModel(string content) : LlmServiceBase(new NoEventBus(), new Clock())
    {
        public string? LastMessage { get; private set; }

        public override string ProviderName => "Fake";

        protected override string DefaultModel => "fake";

        protected override Task<LlmResponse> SendCoreAsync(LlmRequest request, string model, CancellationToken cancellationToken)
        {
            LastMessage = request.Prompt.Message;

            return Task.FromResult(new LlmResponse(content, LlmTokenUsage.None, model));
        }
    }

    private sealed class FakeGateway(LlmServiceBase service) : ILlmGateway
    {
        public Task<LlmClient> ForFeatureAsync(AiFeature feature, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LlmClient(service, feature, new LlmTarget("fake")));
    }

    private sealed class NoEventBus : IEventBus
    {
        public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
            where T : IIntegrationEvent => Task.CompletedTask;
    }

    private sealed class Clock : IDateTimeProvider
    {
        public DateTime UtcNow => Now;
    }
}
