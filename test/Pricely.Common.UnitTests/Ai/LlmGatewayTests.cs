using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Common.Application.AI;
using Pricely.Common.Infrastructure.Ai;

namespace Pricely.Common.UnitTests.Ai;

public sealed class LlmGatewayTests
{
    private static readonly AiFeature Feature = new("pricing.test", "Test", "Pricing", "fake");

    [Fact]
    public async Task ForFeatureAsync_ShouldUseDefaultProvider_WhenNoTargetIsConfigured()
    {
        using ServiceProvider services = Services();
        LlmGateway gateway = new(services, new FixedTargetResolver(null));

        LlmClient client = await gateway.ForFeatureAsync(Feature);

        client.Target.Should().Be(new LlmTarget("fake"));
        client.Feature.Should().Be(Feature);
    }

    [Fact]
    public async Task ForFeatureAsync_ShouldUseConfiguredTarget()
    {
        using ServiceProvider services = Services();
        LlmGateway gateway = new(services, new FixedTargetResolver(new LlmTarget("other", "other-model")));

        LlmClient client = await gateway.ForFeatureAsync(Feature);

        client.Target.Should().Be(new LlmTarget("other", "other-model"));
    }

    [Fact]
    public async Task LlmClient_ShouldSendWithTargetModel_AndCreateSession_WhenPromptHasNone()
    {
        RecordingEventBus eventBus = new();
        FakeLlmService service = new(eventBus);
        LlmClient client = new(service, Feature, new LlmTarget("fake", "fake-large"));

        await client.SendAsync(new LlmPrompt { Message = "Hello" });

        service.LastModel.Should().Be("fake-large");
        eventBus.Published.OfType<LlmCalledIntegrationEvent>().Single().SessionId.Should().NotBeNullOrWhiteSpace();
    }

    private static ServiceProvider Services()
    {
        ServiceCollection services = new();
        RecordingEventBus eventBus = new();
        services.AddKeyedScoped<LlmServiceBase>("fake", (_, _) => new FakeLlmService(eventBus));
        services.AddKeyedScoped<LlmServiceBase>("other", (_, _) => new FakeLlmService(eventBus));

        return services.BuildServiceProvider();
    }
}
