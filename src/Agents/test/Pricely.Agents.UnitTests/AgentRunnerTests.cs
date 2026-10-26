using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Agents.Core.Application;
using Pricely.Agents.Core.Domain;
using Pricely.Agents.Core.Infrastructure;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;

namespace Pricely.Agents.UnitTests;

public sealed class AgentRunnerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public async Task Turn_ShouldRunReadTools_ProposeWriteTools_AndSaveTheConversation()
    {
        var probe = new ToolProbe();
        var eventBus = new RecordingEventBus();
        using var chat = new ScriptedChatClient(ToolCalls(), Answer("Voici le comparatif."));
        await using ServiceProvider services = TestServices.Build(chat, eventBus, probe);

        List<AgentEvent> events = await RunAsync(services, null, "Compare le ciment");

        events.Select(e => e.Type).Should().Equal(
            AgentEvent.Conversation,
            AgentEvent.ToolCall,
            AgentEvent.ToolCall,
            AgentEvent.ToolResult,
            AgentEvent.ActionProposed,
            AgentEvent.Text,
            AgentEvent.Done);

        probe.Calls.Should().Equal("read:ciment");

        var result = (ToolResultEvent)events[3].Data;
        result.Name.Should().Be("read_thing");
        result.Result!.Value.GetString().Should().Be("value-ciment");

        var proposed = (ActionProposedEvent)events[4].Data;
        proposed.ToolName.Should().Be("write_thing");
        proposed.RequiredScope.Should().Be(PricelyScopes.Write);
        proposed.Arguments.GetProperty("productId").GetString().Should().Be(ProductId.ToString());

        // The model was told the write tool is waiting for approval, and saw the read tool result.
        string secondRequest = JsonSerializer.Serialize(chat.Requests[1]);
        secondRequest.Should().Contain("pending_approval").And.Contain("value-ciment");

        using IServiceScope scope = services.CreateScope();
        AgentsDbContext db = scope.ServiceProvider.GetRequiredService<AgentsDbContext>();
        AgentConversation conversation = await db.Conversations.Include(c => c.Messages).SingleAsync();
        conversation.UserId.Should().Be(UserId);
        conversation.Title.Should().Be("Compare le ciment");
        conversation.Messages.Select(m => m.Role).Should().Equal(
            AgentMessageRole.User, AgentMessageRole.Assistant, AgentMessageRole.Tool, AgentMessageRole.Assistant);
        (await db.PendingActions.SingleAsync()).Status.Should().Be(PendingActionStatus.Pending);

        eventBus.Published.OfType<LlmCalledIntegrationEvent>().Should().HaveCount(2)
            .And.OnlyContain(e => e.SessionId == conversation.Id.ToString() && e.FeatureCode == "agents.test");
    }

    [Fact]
    public async Task NextTurn_ShouldReplayTheConversationToTheModel()
    {
        var probe = new ToolProbe();
        using var chat = new ScriptedChatClient(Answer("Bonjour."), Answer("Toujours là."));
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), probe);

        List<AgentEvent> first = await RunAsync(services, null, "Bonjour");
        Guid conversationId = ConversationId(first);

        await RunAsync(services, conversationId, "Tu es là ?");

        chat.Requests[1].Select(m => m.Text).Should().Equal("Bonjour", "Bonjour.", "Tu es là ?");
    }

    [Fact]
    public async Task Approve_ShouldRunTheToolWithTheProposedArguments_AndNoteItForTheModel()
    {
        var probe = new ToolProbe();
        using var chat = new ScriptedChatClient(ToolCalls(), Answer("Je propose la collecte."));
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), probe);
        List<AgentEvent> events = await RunAsync(services, null, "Collecte");
        var proposed = (ActionProposedEvent)events.Single(e => e.Type == AgentEvent.ActionProposed).Data;

        using IServiceScope scope = services.CreateScope();
        AgentActionService actions = scope.ServiceProvider.GetRequiredService<AgentActionService>();

        Result<PendingActionResponse> denied = await actions.ApproveAsync(proposed.ActionId, UserId, new HashSet<string> { PricelyScopes.Read }, CancellationToken.None);
        denied.Error.Code.Should().Be("Actions.MissingScope");
        probe.Calls.Should().NotContain(c => c.StartsWith("write:", StringComparison.Ordinal));

        Result<PendingActionResponse> approved = await actions.ApproveAsync(proposed.ActionId, UserId, new HashSet<string> { PricelyScopes.Write }, CancellationToken.None);

        approved.Value.Status.Should().Be(nameof(PendingActionStatus.Executed));
        approved.Value.Result!.Value.GetProperty("offers").GetInt32().Should().Be(3);
        probe.Calls.Should().Contain("write:" + ProductId);

        Result<PendingActionResponse> again = await actions.ApproveAsync(proposed.ActionId, UserId, new HashSet<string> { PricelyScopes.Write }, CancellationToken.None);
        again.Error.Code.Should().Be("Actions.AlreadyDecided");

        ConversationQueries queries = scope.ServiceProvider.GetRequiredService<ConversationQueries>();
        ConversationResponse conversation = (await queries.GetAsync(ConversationId(events), UserId, CancellationToken.None)).Value;
        conversation.Actions.Single().Status.Should().Be(nameof(PendingActionStatus.Executed));
        conversation.Messages.Should().NotContain(m => m.Role == nameof(AgentMessageRole.System), "notes for the model are not shown");
        conversation.Messages.SelectMany(m => m.ToolCalls).Select(c => c.Name).Should().Equal("read_thing", "write_thing");
    }

    [Fact]
    public async Task Conversation_ShouldOnlyBeVisibleToItsOwner()
    {
        using var chat = new ScriptedChatClient(Answer("Bonjour."));
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), new ToolProbe());
        Guid conversationId = ConversationId(await RunAsync(services, null, "Bonjour"));

        using IServiceScope scope = services.CreateScope();
        ConversationQueries queries = scope.ServiceProvider.GetRequiredService<ConversationQueries>();
        AgentRunner runner = scope.ServiceProvider.GetRequiredService<AgentRunner>();

        (await queries.GetAsync(conversationId, Guid.NewGuid(), CancellationToken.None)).Error.Code.Should().Be("Conversations.NotFound");
        (await queries.ListAsync(Guid.NewGuid(), null, CancellationToken.None)).Should().BeEmpty();
        (await runner.PrepareAsync("test", conversationId, "Salut", Guid.NewGuid(), CancellationToken.None)).Error.Code.Should().Be("Conversations.NotFound");
        (await runner.PrepareAsync("inconnu", null, "Salut", UserId, CancellationToken.None)).Error.Code.Should().Be("Conversations.UnknownAgent");
    }

    [Fact]
    public async Task Approve_ShouldBeRefused_ToAnotherUser()
    {
        using var chat = new ScriptedChatClient(ToolCalls(), Answer("Je propose la collecte."));
        var probe = new ToolProbe();
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), probe);
        var proposed = (ActionProposedEvent)(await RunAsync(services, null, "Collecte")).Single(e => e.Type == AgentEvent.ActionProposed).Data;

        using IServiceScope scope = services.CreateScope();
        AgentActionService actions = scope.ServiceProvider.GetRequiredService<AgentActionService>();

        (await actions.ApproveAsync(proposed.ActionId, Guid.NewGuid(), new HashSet<string> { PricelyScopes.Write }, CancellationToken.None))
            .Error.Code.Should().Be("Actions.NotFound");
        (await actions.RejectAsync(proposed.ActionId, Guid.NewGuid(), CancellationToken.None)).Error.Code.Should().Be("Actions.NotFound");
        probe.Calls.Should().NotContain(c => c.StartsWith("write:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Decision_ShouldBeNotedAsAUserTurn_NotAsASystemInstruction()
    {
        using var chat = new ScriptedChatClient(ToolCalls(), Answer("Je propose la collecte."), Answer("C'est noté."));
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), new ToolProbe());
        List<AgentEvent> events = await RunAsync(services, null, "Collecte");
        var proposed = (ActionProposedEvent)events.Single(e => e.Type == AgentEvent.ActionProposed).Data;

        using (IServiceScope scope = services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AgentActionService>().RejectAsync(proposed.ActionId, UserId, CancellationToken.None);
        }

        await RunAsync(services, ConversationId(events), "Et maintenant ?");

        ChatMessage note = chat.Requests[2][^2];
        note.Role.Should().Be(ChatRole.User);
        note.Text.Should().Contain("refusé").And.StartWith("[Note de Pricely");
        chat.Requests[2].Should().NotContain(m => m.Role == ChatRole.System);
    }

    [Fact]
    public async Task Turn_StoppedByTheRoundTripLimit_ShouldNotLeaveUnansweredToolCalls()
    {
        ChatResponse[] loop = Enumerable.Range(0, AgentRunner.MaximumToolRoundTrips + 1)
            .Select(i => new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                [new FunctionCallContent($"c{i}", "read_thing", new Dictionary<string, object?> { ["text"] = "x" })])))
            .ToArray();
        using var chat = new ScriptedChatClient(loop);
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), new ToolProbe());

        await RunAsync(services, null, "Boucle");

        using IServiceScope scope = services.CreateScope();
        AgentConversation conversation = await scope.ServiceProvider.GetRequiredService<AgentsDbContext>()
            .Conversations.Include(c => c.Messages).SingleAsync();
        var messages = conversation.Messages.Select(m => ChatMessageSerializer.Deserialize(m.Content)).ToList();
        IEnumerable<string> calls = messages.SelectMany(m => m.Contents.OfType<FunctionCallContent>()).Select(c => c.CallId);
        IEnumerable<string> results = messages.SelectMany(m => m.Contents.OfType<FunctionResultContent>()).Select(r => r.CallId);

        results.Should().BeEquivalentTo(calls);
    }

    private static ChatResponse ToolCalls() =>
        new(new ChatMessage(
            ChatRole.Assistant,
            [
                new FunctionCallContent("c1", "read_thing", new Dictionary<string, object?> { ["text"] = "ciment" }),
                new FunctionCallContent("c2", "write_thing", new Dictionary<string, object?> { ["productId"] = ProductId.ToString() })
            ]));

    private static ChatResponse Answer(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    private static Guid ConversationId(List<AgentEvent> events) =>
        (Guid)events[0].Data.GetType().GetProperty("conversationId")!.GetValue(events[0].Data)!;

    private static async Task<List<AgentEvent>> RunAsync(ServiceProvider services, Guid? conversationId, string message)
    {
        using IServiceScope scope = services.CreateScope();
        AgentRunner runner = scope.ServiceProvider.GetRequiredService<AgentRunner>();

        Result<AgentConversation> conversation = await runner.PrepareAsync("test", conversationId, message, UserId, CancellationToken.None);

        var events = new List<AgentEvent>();
        await foreach (AgentEvent agentEvent in runner.RunAsync(conversation.Value, message, CancellationToken.None))
        {
            events.Add(agentEvent);
        }

        return events;
    }
}
