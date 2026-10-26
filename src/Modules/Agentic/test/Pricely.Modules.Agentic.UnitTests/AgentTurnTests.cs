using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.Mcp;
using Pricely.Modules.Agentic.Application;
using Pricely.Modules.Agentic.Application.Actions;
using Pricely.Modules.Agentic.Application.Actions.ApproveAction;
using Pricely.Modules.Agentic.Application.Actions.PublishActionDecided;
using Pricely.Modules.Agentic.Application.Actions.RejectAction;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Application.Conversations;
using Pricely.Modules.Agentic.Application.Conversations.RunTurn;
using Pricely.Modules.Agentic.Application.Conversations.StartTurn;
using Pricely.Modules.Agentic.Domain.Actions;
using Pricely.Modules.Agentic.Domain.Conversations;
using Pricely.Modules.Agentic.Infrastructure.Database;

namespace Pricely.Modules.Agentic.UnitTests;

public sealed class AgentTurnTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public async Task Turn_ShouldRunReadTools_ProposeWriteTools_AndJournalForAiEngine()
    {
        var probe = new ToolProbe();
        var eventBus = new RecordingEventBus();
        using var chat = new ScriptedChatClient(ToolCalls(), Answer("Voici le comparatif."));
        await using ServiceProvider services = TestServices.Build(chat, eventBus, probe, Owner());

        List<AgentEvent> events = await RunAsync(services, null, "Compare le ciment");

        events.Select(e => e.Type).Should().Equal(
            AgentEvent.Conversation, AgentEvent.ToolCall, AgentEvent.ToolCall, AgentEvent.ToolResult,
            AgentEvent.ActionProposed, AgentEvent.Text, AgentEvent.Done);
        probe.Calls.Should().Equal("read:ciment");

        ((ToolResultEvent)events[3].Data).Result!.Value.GetString().Should().Be("value-ciment");
        var proposed = (ActionProposedEvent)events[4].Data;
        proposed.ToolName.Should().Be("write_thing");
        proposed.RequiredScope.Should().Be(TestServices.WriteScope);
        proposed.Arguments.GetProperty("productId").GetString().Should().Be(ProductId.ToString());

        JsonSerializer.Serialize(chat.Requests[1]).Should().Contain("pending_approval").And.Contain("value-ciment");

        AgentConversation conversation = await LoadConversationAsync(services);
        conversation.Messages.OrderBy(m => m.Sequence).Select(m => m.Role).Should().Equal(
            AgentMessageRole.User, AgentMessageRole.Assistant, AgentMessageRole.Tool, AgentMessageRole.Assistant);

        // AiEngine journal: every model call, with the agent, and the read tool call.
        eventBus.Published.OfType<LlmCalledIntegrationEvent>().Should().HaveCount(2)
            .And.OnlyContain(e => e.SessionId == conversation.Id.ToString() && e.AgentName == "test" && e.FeatureCode == "agents.test");
        AiToolInvokedIntegrationEvent invoked = eventBus.Published.OfType<AiToolInvokedIntegrationEvent>().Should().ContainSingle().Subject;
        invoked.ToolName.Should().Be("read_thing");
        invoked.IsSuccess.Should().BeTrue();
        invoked.WasApproved.Should().BeFalse();
        invoked.SessionId.Should().Be(conversation.Id.ToString());
    }

    [Fact]
    public async Task NextTurn_ShouldReplayTheConversationToTheModel()
    {
        using var chat = new ScriptedChatClient(Answer("Bonjour."), Answer("Toujours là."));
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), new ToolProbe(), Owner());

        Guid conversationId = ConversationId(await RunAsync(services, null, "Bonjour"));
        await RunAsync(services, conversationId, "Tu es là ?");

        chat.Requests[1].Select(m => m.Text).Should().Equal("Bonjour", "Bonjour.", "Tu es là ?");
    }

    [Fact]
    public async Task Approve_ShouldRunTheToolOnce_WithTheProposedArguments_AndRaiseTheDecision()
    {
        var probe = new ToolProbe();
        var eventBus = new RecordingEventBus();
        FakeCurrentUser user = Owner();
        using var chat = new ScriptedChatClient(ToolCalls(), Answer("Je propose la collecte."));
        await using ServiceProvider services = TestServices.Build(chat, eventBus, probe, user);
        var proposed = (ActionProposedEvent)(await RunAsync(services, null, "Collecte")).Single(e => e.Type == AgentEvent.ActionProposed).Data;

        (await SendAsync(services, new ApproveActionCommand(proposed.ActionId))).Error.Code.Should().Be("Actions.MissingScope");
        probe.Calls.Should().NotContain(c => c.StartsWith("write:", StringComparison.Ordinal));

        user.Scopes = new HashSet<string> { TestServices.WriteScope };
        Result<PendingActionResponse> approved = await SendAsync(services, new ApproveActionCommand(proposed.ActionId));

        approved.Value.Status.Should().Be(nameof(PendingActionStatus.Executed));
        approved.Value.Result!.Value.GetProperty("offers").GetInt32().Should().Be(3);
        probe.Calls.Should().ContainSingle(c => c == "write:" + ProductId);
        (await SendAsync(services, new ApproveActionCommand(proposed.ActionId))).Error.Code.Should().Be("Actions.AlreadyDecided");

        eventBus.Published.OfType<AiToolInvokedIntegrationEvent>().Should().Contain(e => e.ToolName == "write_thing" && e.WasApproved);

        using IServiceScope scope = services.CreateScope();
        PendingAction action = await scope.ServiceProvider.GetRequiredService<AgenticDbContext>().PendingActions.SingleAsync();
        action.DomainEvents.Should().BeEmpty("the decision event is raised on the tracked action and handled through the outbox");
    }

    [Fact]
    public async Task Decision_ShouldBeRaisedOnTheAction_ForTheOutbox()
    {
        var action = PendingAction.Propose(Guid.NewGuid(), UserId, "test", "write_thing", "{}", TestServices.WriteScope, DateTime.UtcNow);

        action.Approve(UserId, new HashSet<string> { TestServices.WriteScope }, DateTime.UtcNow).IsSuccess.Should().BeTrue();
        action.MarkExecuted("{}");

        action.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PendingActionDecidedDomainEvent>();
    }

    [Fact]
    public async Task Approve_AndReject_ShouldBeRefused_ToAnotherUser()
    {
        var probe = new ToolProbe();
        FakeCurrentUser user = Owner();
        using var chat = new ScriptedChatClient(ToolCalls(), Answer("Je propose la collecte."));
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), probe, user);
        var proposed = (ActionProposedEvent)(await RunAsync(services, null, "Collecte")).Single(e => e.Type == AgentEvent.ActionProposed).Data;

        user.UserId = Guid.NewGuid();
        user.Scopes = new HashSet<string> { TestServices.WriteScope };

        (await SendAsync(services, new ApproveActionCommand(proposed.ActionId))).Error.Code.Should().Be("Actions.NotFound");
        (await SendAsync(services, new RejectActionCommand(proposed.ActionId))).Error.Code.Should().Be("Actions.NotFound");
        (await SendStartAsync(services, ConversationIdOf(services), "Salut")).Error.Code.Should().Be("Conversations.NotFound");
        probe.Calls.Should().NotContain(c => c.StartsWith("write:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Decision_ShouldBeNotedAsAUserTurn_NotAsASystemInstruction()
    {
        using var chat = new ScriptedChatClient(ToolCalls(), Answer("Je propose la collecte."), Answer("C'est noté."));
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), new ToolProbe(), Owner());
        List<AgentEvent> events = await RunAsync(services, null, "Collecte");
        var proposed = (ActionProposedEvent)events.Single(e => e.Type == AgentEvent.ActionProposed).Data;

        await SendAsync(services, new RejectActionCommand(proposed.ActionId));
        await RunAsync(services, ConversationId(events), "Et maintenant ?");

        ChatMessage note = chat.Requests[2][^2];
        note.Role.Should().Be(ChatRole.User);
        note.Text.Should().StartWith("[Note de l'application").And.Contain("refusé");
        chat.Requests[2].Should().NotContain(m => m.Role == ChatRole.System);
    }

    [Fact]
    public async Task Turn_StoppedByTheRoundTripLimit_ShouldNotLeaveUnansweredToolCalls()
    {
        ChatResponse[] loop = Enumerable.Range(0, 11)
            .Select(i => new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                [new FunctionCallContent($"c{i}", "read_thing", new Dictionary<string, object?> { ["text"] = "x" })])))
            .ToArray();
        using var chat = new ScriptedChatClient(loop);
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), new ToolProbe(), Owner());

        await RunAsync(services, null, "Boucle");

        AgentConversation conversation = await LoadConversationAsync(services);
        var messages = conversation.Messages.Select(m => ChatMessageSerializer.Deserialize(m.Content)).ToList();
        IEnumerable<string> calls = messages.SelectMany(m => m.Contents.OfType<FunctionCallContent>()).Select(c => c.CallId);
        IEnumerable<string> results = messages.SelectMany(m => m.Contents.OfType<FunctionResultContent>()).Select(r => r.CallId);

        results.Should().BeEquivalentTo(calls);
    }

    [Fact]
    public async Task Start_ShouldRefuse_AnUnknownAgent_OrAnUnknownUser()
    {
        FakeCurrentUser user = Owner();
        using var chat = new ScriptedChatClient();
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), new ToolProbe(), user);

        (await SendStartAsync(services, null, "Salut", "inconnu")).Error.Code.Should().Be("Conversations.UnknownAgent");

        user.UserId = null;
        (await SendStartAsync(services, null, "Salut")).Error.Code.Should().Be("Conversations.UnknownUser");
    }

    [Fact]
    public async Task Approve_ShouldRecordTheFailure_WhenTheToolFails()
    {
        var probe = new ToolProbe();
        var eventBus = new RecordingEventBus();
        FakeCurrentUser user = Owner();
        user.Scopes = new HashSet<string> { TestServices.WriteScope };
        using var chat = new ScriptedChatClient();
        await using ServiceProvider services = TestServices.Build(chat, eventBus, probe, user);
        Guid actionId = await InsertActionAsync(services, "fail_thing", """{"text":"x"}""");

        Result<PendingActionResponse> approved = await SendAsync(services, new ApproveActionCommand(actionId));

        approved.Value.Status.Should().Be(nameof(PendingActionStatus.Failed));
        probe.Calls.Should().Equal("fail:x");
        AiToolInvokedIntegrationEvent invoked = eventBus.Published.OfType<AiToolInvokedIntegrationEvent>().Should().ContainSingle().Subject;
        invoked.IsSuccess.Should().BeFalse();
        invoked.WasApproved.Should().BeTrue();
        (await SendAsync(services, new ApproveActionCommand(actionId))).Error.Code.Should().Be("Actions.AlreadyDecided");
    }

    [Fact]
    public async Task Approve_ShouldRefuse_AReadOnlyTool()
    {
        var probe = new ToolProbe();
        FakeCurrentUser user = Owner();
        user.Scopes = new HashSet<string> { TestServices.WriteScope };
        using var chat = new ScriptedChatClient();
        await using ServiceProvider services = TestServices.Build(chat, new RecordingEventBus(), probe, user);
        Guid actionId = await InsertActionAsync(services, "read_thing", """{"text":"x"}""");

        (await SendAsync(services, new ApproveActionCommand(actionId))).Error.Code.Should().Be("Actions.UnknownTool");
        probe.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Decision_ShouldBePublishedToTheAiEngineJournal()
    {
        var eventBus = new RecordingEventBus();
        using var chat = new ScriptedChatClient();
        await using ServiceProvider services = TestServices.Build(chat, eventBus, new ToolProbe(), Owner());
        Guid actionId = await InsertActionAsync(services, "write_thing", "{}");
        await SendAsync(services, new RejectActionCommand(actionId));

        using (IServiceScope scope = services.CreateScope())
        {
            await ActivatorUtilities.CreateInstance<PendingActionDecidedDomainEventHandler>(scope.ServiceProvider)
                .Handle(new PendingActionDecidedDomainEvent(actionId));
        }

        AiActionDecidedIntegrationEvent decided = eventBus.Published.OfType<AiActionDecidedIntegrationEvent>().Should().ContainSingle().Subject;
        decided.ActionId.Should().Be(actionId);
        decided.Decision.Should().Be(AiActionDecidedIntegrationEvent.Rejected);
        decided.AgentName.Should().Be("test");
        decided.FeatureCode.Should().Be("agents.test");
        decided.SourceModule.Should().Be("Agents");
        decided.ToolName.Should().Be("write_thing");
        decided.UserId.Should().Be(UserId.ToString());
        decided.DecidedOnUtc.Should().Be(new FixedClock().UtcNow);
    }

    [Fact]
    public void Catalog_ShouldRefuse_AnAgentNamingAnUnknownTool()
    {
        AgentDefinition agent = TestAgent.Definition with { ToolNames = ["read_thing", "unknown_tool"] };

        Func<AgentCatalog> build = () => new AgentCatalog([agent], McpServerExtensions.ToAgentTools([typeof(TestTools).Assembly]), new AgenticOptions());

        build.Should().Throw<InvalidOperationException>().WithMessage("*test/unknown_tool*");
    }

    private static FakeCurrentUser Owner() => new() { UserId = UserId };

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

    private static Guid ConversationIdOf(ServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();

        return scope.ServiceProvider.GetRequiredService<AgenticDbContext>().Conversations.Single().Id;
    }

    private static async Task<Guid> InsertActionAsync(ServiceProvider services, string toolName, string arguments)
    {
        using IServiceScope scope = services.CreateScope();
        AgenticDbContext dbContext = scope.ServiceProvider.GetRequiredService<AgenticDbContext>();
        var action = PendingAction.Propose(Guid.NewGuid(), UserId, "test", toolName, arguments, TestServices.WriteScope, DateTime.UtcNow);
        dbContext.PendingActions.Add(action);
        await dbContext.SaveChangesAsync();

        return action.Id;
    }

    private static async Task<AgentConversation> LoadConversationAsync(ServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<AgenticDbContext>().Conversations.Include(c => c.Messages).SingleAsync();
    }

    private static async Task<Result<PendingActionResponse>> SendAsync(ServiceProvider services, IRequest<Result<PendingActionResponse>> command)
    {
        using IServiceScope scope = services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    private static async Task<Result<TurnStartedResponse>> SendStartAsync(ServiceProvider services, Guid? conversationId, string message, string agent = "test")
    {
        using IServiceScope scope = services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new StartTurnCommand(agent, conversationId, message));
    }

    private static async Task<List<AgentEvent>> RunAsync(ServiceProvider services, Guid? conversationId, string message)
    {
        Result<TurnStartedResponse> started = await SendStartAsync(services, conversationId, message);

        using IServiceScope scope = services.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var events = new List<AgentEvent>();
        await foreach (AgentEvent agentEvent in sender.CreateStream(new RunTurnStreamRequest(started.Value.ConversationId, message)))
        {
            events.Add(agentEvent);
        }

        return events;
    }
}
