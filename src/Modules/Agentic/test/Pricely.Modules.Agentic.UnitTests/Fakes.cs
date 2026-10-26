using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Presentation.Mcp;
using Pricely.Modules.Agentic.Application;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Infrastructure;
using Pricely.Modules.Agentic.Infrastructure.Database;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;

namespace Pricely.Modules.Agentic.UnitTests;

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

    [McpServerTool(Name = "fail_thing", ReadOnly = false)]
    [Description("Échoue.")]
    public static string FailThing(IToolProbe probe, [Description("Texte.")] string text)
    {
        probe.Calls.Add("fail:" + text);

        throw new InvalidOperationException("The merchant page could not be read.");
    }
}

public sealed record WriteResult(Guid ProductId, int Offers);

internal static class TestAgent
{
    public static readonly AgentDefinition Definition = new(
        "test",
        new AiFeature("agents.test", "Test", "Agents", "fake"),
        "Tu es un agent de test.",
        ["read_thing", "write_thing", "fail_thing"]);
}

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }

    public IReadOnlySet<string> Scopes { get; set; } = new HashSet<string>();
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

/// <summary>A second later at each reading: the order of the timestamps matters.</summary>
internal sealed class TickingClock : IDateTimeProvider
{
    private int _ticks;

    public DateTime UtcNow => new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc).AddSeconds(Interlocked.Increment(ref _ticks));
}

internal static class TestServices
{
    public const string WriteScope = "pricely.write";

    public static ServiceProvider Build(
        ScriptedChatClient chatClient,
        RecordingEventBus eventBus,
        ToolProbe probe,
        FakeCurrentUser user,
        IDateTimeProvider? clock = null)
    {
        var services = new ServiceCollection();
        string database = Guid.NewGuid().ToString();

        services.AddLogging();
        services.AddDbContext<AgenticDbContext>(options => options.UseInMemoryDatabase(database));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AgenticDbContext>());
        services.AddAgenticCore(new AgenticOptions { DefaultApprovalScope = WriteScope });
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly));
        services.AddAgent(TestAgent.Definition);
        services.AddAgentTools(McpServerExtensions.ToAgentTools([typeof(TestTools).Assembly]));
        services.AddSingleton(clock ?? new FixedClock());
        services.AddSingleton<IEventBus>(eventBus);
        services.AddSingleton<ICurrentUser>(user);
        services.AddSingleton<IToolProbe>(probe);
        services.AddSingleton<ILlmGateway>(sp => new FakeGateway(new FakeLlmService(chatClient, eventBus, sp.GetRequiredService<IDateTimeProvider>())));

        return services.BuildServiceProvider();
    }
}
