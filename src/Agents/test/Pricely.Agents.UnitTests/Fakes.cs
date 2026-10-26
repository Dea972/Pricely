using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Pricely.Agents.Core.Application;
using Pricely.Agents.Core.Infrastructure;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;

namespace Pricely.Agents.UnitTests;

public interface IToolProbe
{
    List<string> Calls { get; }
}

internal sealed class ToolProbe : IToolProbe
{
    public List<string> Calls { get; } = [];
}

[McpServerToolType]
public static class TestTools
{
    [McpServerTool(Name = "read_thing", ReadOnly = true)]
    [Description("Lit une chose.")]
    public static string ReadThing(IToolProbe probe, [Description("Texte.")] string text)
    {
        probe.Calls.Add("read:" + text);

        return "value-" + text;
    }

    [McpServerTool(Name = "write_thing", ReadOnly = false)]
    [Description("Écrit une chose.")]
    public static WriteResult WriteThing(IToolProbe probe, [Description("Produit.")] Guid productId)
    {
        probe.Calls.Add("write:" + productId);

        return new WriteResult(productId, 3);
    }
}

public sealed record WriteResult(Guid ProductId, int Offers);

internal sealed class TestAgent : IAgentDefinition
{
    public string Name => "test";

    public AiFeature Feature { get; } = new("agents.test", "Test", "Agents", "fake");

    public string Instructions => "Tu es un agent de test.";

    public IReadOnlyCollection<string> ToolNames { get; } = ["read_thing", "write_thing", "unknown_tool"];
}

/// <summary>Answers each model call with the next scripted response.</summary>
internal sealed class ScriptedChatClient(params ChatResponse[] responses) : IChatClient
{
    private readonly Queue<ChatResponse> _responses = new(responses);

    public List<List<ChatMessage>> Requests { get; } = [];

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Requests.Add([.. messages]);

        return Task.FromResult(_responses.Dequeue());
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

internal sealed class FakeLlmService(IChatClient chatClient, IEventBus eventBus, IDateTimeProvider clock) : LlmServiceBase(eventBus, clock)
{
    public override string ProviderName => "Fake";

    protected override string DefaultModel => "fake-model";

    protected override Task<LlmResponse> SendCoreAsync(LlmRequest request, string model, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    protected override IChatClient CreateChatClientCore(string model) => chatClient;
}

internal sealed class FakeGateway(LlmServiceBase service) : ILlmGateway
{
    public Task<LlmClient> ForFeatureAsync(AiFeature feature, CancellationToken cancellationToken = default) =>
        Task.FromResult(new LlmClient(service, feature, new LlmTarget("fake")));
}

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

internal sealed class FixedClock : IDateTimeProvider
{
    public DateTime UtcNow { get; } = new(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc);
}

internal static class TestServices
{
    public static ServiceProvider Build(ScriptedChatClient chatClient, RecordingEventBus eventBus, ToolProbe probe)
    {
        var services = new ServiceCollection();
        string database = Guid.NewGuid().ToString();

        services.AddLogging();
        services.AddDbContext<AgentsDbContext>(options => options.UseInMemoryDatabase(database));
        services.AddSingleton(new AgentToolCatalog([typeof(TestTools).Assembly]));
        services.AddSingleton<IAgentDefinition, TestAgent>();
        services.AddSingleton<IDateTimeProvider, FixedClock>();
        services.AddSingleton<IToolProbe>(probe);
        services.AddSingleton<ILlmGateway>(sp => new FakeGateway(new FakeLlmService(chatClient, eventBus, sp.GetRequiredService<IDateTimeProvider>())));
        services.AddScoped<AgentRunner>();
        services.AddScoped<AgentActionService>();
        services.AddScoped<ConversationQueries>();

        return services.BuildServiceProvider();
    }
}
