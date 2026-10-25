using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;

namespace Pricely.Common.UnitTests.Ai;

internal sealed class RecordingEventBus : IEventBus
{
    public List<IIntegrationEvent> Published { get; } = [];

    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        Published.Add(integrationEvent);

        return Task.CompletedTask;
    }
}

internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; } = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
}

internal sealed class FakeLlmService(RecordingEventBus eventBus, Exception? failure = null)
    : LlmServiceBase(eventBus, new FixedDateTimeProvider())
{
    public const string FakeDefaultModel = "fake-default";

    public string? LastModel { get; private set; }

    public override string ProviderName => "Fake";

    protected override string DefaultModel => FakeDefaultModel;

    protected override Task<LlmResponse> SendCoreAsync(LlmRequest request, string model, CancellationToken cancellationToken)
    {
        LastModel = model;

        return failure is null
            ? Task.FromResult(new LlmResponse("answer", new LlmTokenUsage(10, 2, 1, 5, 0), model))
            : Task.FromException<LlmResponse>(failure);
    }
}

internal sealed class FixedTargetResolver(LlmTarget? target) : ILlmTargetResolver
{
    public Task<LlmTarget?> ResolveAsync(AiFeature feature, CancellationToken cancellationToken = default) =>
        Task.FromResult(target);
}
