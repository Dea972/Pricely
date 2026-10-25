using FluentAssertions;
using Pricely.Common.Application.AI;
using Pricely.Common.Domain;

namespace Pricely.Common.UnitTests.Ai;

public sealed class LlmServiceBaseTests
{
    private static readonly AiFeature Feature = new("pricing.test", "Test", "Pricing", "fake");

    [Fact]
    public async Task SendAsync_ShouldReturnResponse_AndPublishSuccessfulCall()
    {
        RecordingEventBus eventBus = new();
        FakeLlmService service = new(eventBus);

        Result<LlmResponse> result = await service.SendAsync(Request(model: "fake-large", sessionId: "session-1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("answer");
        LlmCalledIntegrationEvent call = eventBus.Published.OfType<LlmCalledIntegrationEvent>().Single();
        call.IsSuccess.Should().BeTrue();
        call.FeatureCode.Should().Be("pricing.test");
        call.SourceModule.Should().Be("Pricing");
        call.SessionId.Should().Be("session-1");
        call.ModelProvider.Should().Be("Fake");
        call.ModelName.Should().Be("fake-large");
        call.InputTokens.Should().Be(10);
        call.OutputTokens.Should().Be(5);
        call.OccurredOnUtc.Should().Be(new FixedDateTimeProvider().UtcNow);
    }

    [Fact]
    public async Task SendAsync_ShouldUseDefaultModel_WhenRequestHasNone()
    {
        RecordingEventBus eventBus = new();
        FakeLlmService service = new(eventBus);

        await service.SendAsync(Request(model: null, sessionId: "session-1"));

        service.LastModel.Should().Be(FakeLlmService.FakeDefaultModel);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFailure_AndPublishFailedCall_WhenProviderThrows()
    {
        RecordingEventBus eventBus = new();
        FakeLlmService service = new(eventBus, new HttpRequestException("boom"));

        Result<LlmResponse> result = await service.SendAsync(Request(model: null, sessionId: "session-1"));

        result.Error.Code.Should().Be("Llm.CallFailed");
        LlmCalledIntegrationEvent call = eventBus.Published.OfType<LlmCalledIntegrationEvent>().Single();
        call.IsSuccess.Should().BeFalse();
        call.ErrorMessage.Should().Be("boom");
    }

    private static LlmRequest Request(string? model, string sessionId) =>
        new()
        {
            Feature = Feature,
            Prompt = new LlmPrompt { Message = "Hello" },
            Model = model,
            SessionId = sessionId
        };
}
