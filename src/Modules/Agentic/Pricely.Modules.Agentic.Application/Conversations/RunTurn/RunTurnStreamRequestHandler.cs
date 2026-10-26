using System.Runtime.CompilerServices;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Clock;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Application.Tools;
using Pricely.Modules.Agentic.Domain.Actions;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Application.Conversations.RunTurn;

/// <summary>
/// One turn: the user message, then the model with its tools until it answers (ADR-008). Read-only tools run right away
/// (journaled for AiEngine); write tools become pending actions, saved as soon as they are proposed. The turn is saved
/// at the end, even if the client went away.
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
            yield return new AgentEvent(AgentEvent.Error, new { code = "Conversations.NotFound", message = "Conversation introuvable." });
            yield break;
        }

        yield return new AgentEvent(AgentEvent.Conversation, new { conversationId = conversation.Id, title = conversation.Title });

        List<ChatMessage> history = [.. conversation.Messages.OrderBy(m => m.Sequence).Select(m => ChatMessageSerializer.Deserialize(m.Content))];
        var userMessage = new ChatMessage(ChatRole.User, request.Message.Trim());
        history.Add(userMessage);
        conversation.Append(AgentMessageRole.User, ChatMessageSerializer.Serialize(userMessage), dateTimeProvider.UtcNow);

        string userId = conversation.UserId.ToString();
        var context = new ToolCallContext(agent.Feature, agent.Name, conversation.Id.ToString(), userId, WasApproved: false);
        var proposals = new Dictionary<string, PendingAction>(StringComparer.Ordinal);
        var tools = catalog.ToolsOf(agent)
            .Select(tool => tool.IsReadOnly
                ? journal.Wrap(tool, context)
                : (AITool)new ProposalFunction(tool, (t, arguments, ct) => ProposeAsync(conversation, agent, t, arguments, proposals, ct)))
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
        string? failure = null;

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
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogWarning(exception, "The turn of conversation {ConversationId} failed", conversation.Id);
                    failure = "Le modèle n'a pas pu répondre. Réessaie dans un instant.";
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
            yield return new AgentEvent(AgentEvent.Error, new { code = "Agents.ModelFailed", message = failure });
        }

        foreach (ChatMessage produced in CloseUnansweredToolCalls(updates.ToChatResponse().Messages))
        {
            conversation.Append(ChatMessageSerializer.ToRole(produced.Role), ChatMessageSerializer.Serialize(produced), dateTimeProvider.UtcNow);
        }

        string? saveFailure = null;
        try
        {
            // Saved even if the client went away: the tools have run and the model calls are billed.
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
        catch (ConcurrentUpdateException exception)
        {
            _logger.LogWarning(exception, "The turn of conversation {ConversationId} could not be saved", conversation.Id);
            saveFailure = "La conversation a été modifiée en même temps ailleurs : ce tour n'a pas été enregistré.";
        }

        if (saveFailure is not null)
        {
            yield return new AgentEvent(AgentEvent.Error, new { code = "Conversations.Concurrent", message = saveFailure });
        }

        yield return new AgentEvent(AgentEvent.Done, new { conversationId = conversation.Id });
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
        AgentDefinition agent,
        AgentTool tool,
        AIFunctionArguments arguments,
        Dictionary<string, PendingAction> proposals,
        CancellationToken cancellationToken)
    {
        var action = PendingAction.Propose(
            conversation.Id,
            conversation.UserId,
            agent.Name,
            tool.Name,
            ChatMessageSerializer.SerializeValue(arguments.ToDictionary(a => a.Key, a => a.Value, StringComparer.Ordinal)),
            catalog.ApprovalScopeOf(tool),
            dateTimeProvider.UtcNow);

        pendingActions.Insert(action);
        proposals[FunctionInvokingChatClient.CurrentContext?.CallContent.CallId ?? action.Id.ToString()] = action;

        // Saved now (with the conversation and its first messages): the user can approve it as soon as it is announced.
        await unitOfWork.SaveChangesAsync(cancellationToken);

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
                        new ActionProposedEvent(action.Id, result.CallId, action.ToolName, JsonDocument.Parse(action.Arguments).RootElement.Clone(), action.RequiredScope));
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
}
