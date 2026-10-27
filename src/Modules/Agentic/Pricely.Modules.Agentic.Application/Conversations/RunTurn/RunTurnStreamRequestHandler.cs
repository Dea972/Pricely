using System.Runtime.CompilerServices;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Clock;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Application.Tools;
using Pricely.Modules.Agentic.Domain.Actions;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Application.Conversations.RunTurn;

/// <summary>
/// One turn: the user message, then the model with its tools until it answers (ADR-008). Read-only tools run right away
/// (journaled for AiEngine); write tools become pending actions, each saved alone as soon as it is proposed. The
/// conversation is written once, at the end of the turn, even if the client went away (ADR-014).
/// </summary>
internal sealed class RunTurnStreamRequestHandler(
    AgentCatalog catalog,
    IAgentConversationRepository conversations,
    IPendingActionRepository pendingActions,
    IUnitOfWork unitOfWork,
    ILlmGateway llmGateway,
    ToolInvocationJournal journal,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    AgenticOptions options,
    IServiceProvider services,
    ILoggerFactory loggerFactory)
    : IStreamRequestHandler<RunTurnStreamRequest, AgentEvent>
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RunTurnStreamRequestHandler>();

    public async IAsyncEnumerable<AgentEvent> Handle(
        RunTurnStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        AgentConversation? conversation = await conversations.GetAsync(request.ConversationId, cancellationToken);
        AgentDefinition? agent = conversation is null ? null : catalog.FindAgent(conversation.AgentName);

        if (conversation is null || agent is null || currentUser.UserId != conversation.UserId)
        {
            yield return ErrorEvent(AgentConversationErrors.NotFound(request.ConversationId));
            yield break;
        }

        Result validation = AgentConversation.ValidateUserMessage(request.Message);
        if (validation.IsFailure)
        {
            yield return ErrorEvent(validation.Error);
            yield break;
        }

        yield return new AgentEvent(AgentEvent.Conversation, new { conversationId = conversation.Id, title = conversation.Title });

        var userMessage = new ChatMessage(ChatRole.User, request.Message.Trim());
        DateTime userMessageOnUtc = dateTimeProvider.UtcNow;
        List<ChatMessage> history = await HistoryAsync(conversation, cancellationToken);
        history.Add(userMessage);

        string userId = conversation.UserId.ToString();
        var context = new ToolCallContext(agent.Feature, agent.Name, conversation.Id.ToString(), userId, WasApproved: false);
        var proposals = new Dictionary<string, PendingAction>(StringComparer.Ordinal);

        // Every write the model proposes in this turn joins one batch, approved or rejected at once (ADR-028).
        var batch = new TurnBatch();
        var tools = catalog.ToolsOf(agent)
            .Select(tool => tool.IsReadOnly
                ? journal.Wrap(tool, context)
                : (AITool)new ProposalFunction(tool, (t, arguments, ct) => ProposeAsync(conversation, batch, agent, t, arguments, proposals, ct)))
            .ToList();

        LlmClient llm = await llmGateway.ForFeatureAsync(agent.Feature, cancellationToken);
        using var chatClient = new FunctionInvokingChatClient(llm.AsChatClient(context.SessionId, userId, agent.Name), loggerFactory, services)
        {
            MaximumIterationsPerRequest = options.MaximumToolRoundTrips,
            IncludeDetailedErrors = false
        };

        var chatOptions = new ChatOptions { Instructions = agent.Instructions, Tools = tools };
        var updates = new List<ChatResponseUpdate>();
        var callNames = new Dictionary<string, string>(StringComparer.Ordinal);
        Error? failure = null;
        bool saveAttempted = false;

        try
        {
            await using (IAsyncEnumerator<ChatResponseUpdate> stream = chatClient
                .GetStreamingResponseAsync(history, chatOptions, cancellationToken)
                .GetAsyncEnumerator(cancellationToken))
            {
                while (true)
                {
                    ChatResponseUpdate update;
                    try
                    {
                        if (!await stream.MoveNextAsync())
                        {
                            break;
                        }

                        update = stream.Current;
                    }
                    catch (OperationCanceledException)
                    {
                        // The client went away: the turn stops, what it did is still saved (finally).
                        break;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "The turn of conversation {ConversationId} failed", conversation.Id);
                        failure = AgentConversationErrors.ModelFailed;
                        break;
                    }

                    updates.Add(update);

                    foreach (AgentEvent agentEvent in ToEvents(update, callNames, proposals))
                    {
                        yield return agentEvent;
                    }
                }
            }

            if (failure is not null)
            {
                yield return ErrorEvent(failure);
            }

            saveAttempted = true;

            if (!await SaveTurnAsync(conversation, userMessage, userMessageOnUtc, updates))
            {
                yield return ErrorEvent(AgentConversationErrors.ConcurrentTurn);
            }

            yield return new AgentEvent(AgentEvent.Done, new { conversationId = conversation.Id });
        }
        finally
        {
            // Also reached when the client stops reading: the tools have run and the model calls are billed.
            if (!saveAttempted)
            {
                await SaveTurnAsync(conversation, userMessage, userMessageOnUtc, updates);
            }
        }
    }

    private static AgentEvent ErrorEvent(Error error) =>
        new(AgentEvent.Error, new { code = error.Code, message = error.Description });

    /// <summary>
    /// The messages of the conversation and, at the time of each decision, a note on the decided actions.
    /// </summary>
    private async Task<List<ChatMessage>> HistoryAsync(AgentConversation conversation, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PendingAction> decided = await pendingActions.GetDecidedAsync(conversation.Id, cancellationToken);

        return conversation.Messages
            .Select(m => (At: m.CreatedOnUtc, Order: m.Sequence, Message: ChatMessageSerializer.Deserialize(m.Content)))
            .Concat(decided.Select(a => (At: NoteTime(conversation, a), Order: int.MaxValue, Message: ConversationNotes.ForDecision(a))))
            .OrderBy(m => m.At)
            .ThenBy(m => m.Order)
            .Select(m => m.Message)
            .ToList();
    }

    /// <summary>
    /// An action can be decided while the turn that proposed it is still running, before that turn's messages are
    /// saved: its note never comes before them (the model would read the decision before the proposal).
    /// </summary>
    private static DateTime NoteTime(AgentConversation conversation, PendingAction action)
    {
        DateTime decidedOnUtc = action.DecidedOnUtc!.Value;
        DateTime? proposingTurnSavedOnUtc = conversation.Messages
            .Where(m => m.Role != AgentMessageRole.User && m.CreatedOnUtc >= action.ProposedOnUtc)
            .Select(m => (DateTime?)m.CreatedOnUtc)
            .Min();

        return proposingTurnSavedOnUtc > decidedOnUtc ? proposingTurnSavedOnUtc.Value : decidedOnUtc;
    }

    /// <summary>
    /// Appends the user message and what the model produced, then saves the conversation (the only aggregate written
    /// at the end of a turn). False when another turn saved the conversation in the meantime.
    /// </summary>
    private async Task<bool> SaveTurnAsync(
        AgentConversation conversation,
        ChatMessage userMessage,
        DateTime userMessageOnUtc,
        List<ChatResponseUpdate> updates)
    {
        conversation.Append(AgentMessageRole.User, ChatMessageSerializer.Serialize(userMessage), userMessageOnUtc);

        // One time for everything the model produced in the turn: the notes of its actions go after all of it.
        DateTime producedOnUtc = dateTimeProvider.UtcNow;
        foreach (ChatMessage produced in CloseUnansweredToolCalls(updates.ToChatResponse().Messages))
        {
            conversation.Append(ChatMessageSerializer.ToRole(produced.Role), ChatMessageSerializer.Serialize(produced), producedOnUtc);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(CancellationToken.None);

            return true;
        }
        catch (ConcurrentUpdateException exception)
        {
            _logger.LogWarning(exception, "The turn of conversation {ConversationId} could not be saved", conversation.Id);

            return false;
        }
    }

    /// <summary>
    /// A turn stopped by the round-trip limit or by an error can end on tool calls without results, which the model API
    /// refuses on the next turn: they are answered as not executed.
    /// </summary>
    private static IList<ChatMessage> CloseUnansweredToolCalls(IList<ChatMessage> messages)
    {
        var answered = messages.SelectMany(m => m.Contents.OfType<FunctionResultContent>()).Select(r => r.CallId).ToHashSet(StringComparer.Ordinal);
        var unanswered = messages
            .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
            .Where(c => !answered.Contains(c.CallId))
            .Select(c => (AIContent)new FunctionResultContent(c.CallId, "Non exécuté : le tour s'est arrêté avant."))
            .ToList();

        return unanswered.Count == 0 ? messages : [.. messages, new ChatMessage(ChatRole.Tool, unanswered)];
    }

    private async Task<PendingAction> ProposeAsync(
        AgentConversation conversation,
        TurnBatch batch,
        AgentDefinition agent,
        AgentTool tool,
        AIFunctionArguments arguments,
        Dictionary<string, PendingAction> proposals,
        CancellationToken cancellationToken)
    {
        // A batch the user decided while the turn still runs is closed: what comes next is decided apart, so that a batch
        // is never approved before its last action was proposed.
        if (batch.Count > 0 && await pendingActions.IsBatchDecidedAsync(batch.Id, cancellationToken))
        {
            batch.Start();
        }

        var action = PendingAction.Propose(
            conversation.Id,
            batch.Id,
            batch.Count,
            conversation.UserId,
            agent.Name,
            new ProposedCall(
                tool.Name,
                ChatMessageSerializer.SerializeValue(arguments.ToDictionary(a => a.Key, a => a.Value, StringComparer.Ordinal)),
                catalog.ApprovalScopeOf(tool)),
            dateTimeProvider.UtcNow);

        pendingActions.Insert(action);
        batch.Count++;

        // Saved now, alone (the conversation is only written at the end of the turn): the user can approve it as soon as
        // it is announced, and it is announced only once saved.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        proposals[FunctionInvokingChatClient.CurrentContext?.CallContent.CallId ?? action.Id.ToString()] = action;

        return action;
    }

    private IEnumerable<AgentEvent> ToEvents(
        ChatResponseUpdate update,
        Dictionary<string, string> callNames,
        Dictionary<string, PendingAction> proposals)
    {
        foreach (AIContent content in update.Contents)
        {
            switch (content)
            {
                case TextContent text when !string.IsNullOrEmpty(text.Text):
                    yield return new AgentEvent(AgentEvent.Text, new { text = text.Text });
                    break;
                case FunctionCallContent call:
                    callNames[call.CallId] = call.Name;
                    yield return new AgentEvent(
                        AgentEvent.ToolCall,
                        new ToolCallEvent(call.CallId, call.Name, catalog.FindTool(call.Name)?.IsReadOnly ?? true, call.Arguments));
                    break;
                case FunctionResultContent result when proposals.TryGetValue(result.CallId, out PendingAction? action):
                    yield return new AgentEvent(
                        AgentEvent.ActionProposed,
                        new ActionProposedEvent(action.Id, result.CallId, action.ToolName, JsonDocument.Parse(action.Arguments).RootElement.Clone(), action.RequiredScope)
                        {
                            BatchId = action.BatchId
                        });
                    break;
                case FunctionResultContent result:
                    yield return new AgentEvent(
                        AgentEvent.ToolResult,
                        new ToolResultEvent(
                            result.CallId,
                            callNames.GetValueOrDefault(result.CallId, string.Empty),
                            result.Exception is null ? ChatMessageSerializer.ToElement(result.Result) : null,
                            result.Exception?.Message));
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>The batch the writes of the turn join (the tools of a turn are called one after the other).</summary>
    private sealed class TurnBatch
    {
        public Guid Id { get; private set; } = Guid.CreateVersion7();

        /// <summary>Actions proposed in the batch so far: the position of the next one.</summary>
        public int Count { get; set; }

        public void Start()
        {
            Id = Guid.CreateVersion7();
            Count = 0;
        }
    }
}
