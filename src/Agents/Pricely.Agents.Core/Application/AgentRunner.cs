using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pricely.Agents.Core.Domain;
using Pricely.Agents.Core.Infrastructure;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Domain;

namespace Pricely.Agents.Core.Application;

/// <summary>
/// Runs one turn of a conversation: the user message, then the model with its tools until it answers (ADR-008).
/// Read-only tools run right away; write tools become pending actions. Everything the turn produced is saved at the end.
/// </summary>
internal sealed class AgentRunner(
    IEnumerable<IAgentDefinition> agents,
    AgentToolCatalog toolCatalog,
    ILlmGateway llmGateway,
    AgentsDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IServiceProvider services,
    ILoggerFactory loggerFactory,
    ILogger<AgentRunner> logger)
{
    public const int MaximumToolRoundTrips = 10;

    public async Task<Result<AgentConversation>> PrepareAsync(
        string agentName,
        Guid? conversationId,
        string message,
        Guid userId,
        CancellationToken cancellationToken)
    {
        IAgentDefinition? agent = FindAgent(agentName);
        if (agent is null)
        {
            return Result.Failure<AgentConversation>(AgentConversationErrors.UnknownAgent(agentName));
        }

        Result validation = AgentConversation.ValidateUserMessage(message);
        if (validation.IsFailure)
        {
            return Result.Failure<AgentConversation>(validation.Error);
        }

        if (conversationId is null)
        {
            Result<AgentConversation> started = AgentConversation.Start(agent.Name, userId, message, dateTimeProvider.UtcNow);
            if (started.IsSuccess)
            {
                dbContext.Conversations.Add(started.Value);
            }

            return started;
        }

        AgentConversation? conversation = await dbContext.Conversations
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation is null || conversation.EnsureOwnedBy(userId).IsFailure)
        {
            return Result.Failure<AgentConversation>(AgentConversationErrors.NotFound(conversationId.Value));
        }

        return string.Equals(conversation.AgentName, agent.Name, StringComparison.Ordinal)
            ? conversation
            : Result.Failure<AgentConversation>(AgentConversationErrors.AgentMismatch(conversation.Id, agent.Name));
    }

    public async IAsyncEnumerable<AgentEvent> RunAsync(
        AgentConversation conversation,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAgentDefinition agent = FindAgent(conversation.AgentName)!;

        yield return new AgentEvent(AgentEvent.Conversation, new { conversationId = conversation.Id, title = conversation.Title });

        List<ChatMessage> history = [.. conversation.Messages.OrderBy(m => m.Sequence).Select(m => ChatMessageSerializer.Deserialize(m.Content))];
        var userMessage = new ChatMessage(ChatRole.User, message.Trim());
        history.Add(userMessage);
        conversation.Append(AgentMessageRole.User, ChatMessageSerializer.Serialize(userMessage), dateTimeProvider.UtcNow);

        var proposals = new Dictionary<string, PendingAction>(StringComparer.Ordinal);
        var tools = new List<AITool>();
        foreach (string toolName in agent.ToolNames)
        {
            AgentTool? tool = toolCatalog.Find(toolName);
            if (tool is not null)
            {
                tools.Add(tool.IsReadOnly ? tool.Function : new ProposalFunction(tool, (t, arguments, ct) => ProposeAsync(conversation, t, arguments, proposals, ct)));
            }
        }

        LlmClient llm = await llmGateway.ForFeatureAsync(agent.Feature, cancellationToken);
        using var chatClient = new FunctionInvokingChatClient(
            llm.AsChatClient(conversation.Id.ToString(), conversation.UserId.ToString()),
            loggerFactory,
            services)
        {
            MaximumIterationsPerRequest = MaximumToolRoundTrips,
            IncludeDetailedErrors = false
        };

        var options = new ChatOptions { Instructions = agent.Instructions, Tools = tools };
        var updates = new List<ChatResponseUpdate>();
        var callNames = new Dictionary<string, string>(StringComparer.Ordinal);
        string? failure = null;

        await using (IAsyncEnumerator<ChatResponseUpdate> stream = chatClient
            .GetStreamingResponseAsync(history, options, cancellationToken)
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
                    logger.LogWarning(exception, "The turn of conversation {ConversationId} failed", conversation.Id);
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

        // Saved even if the client went away: the tools have run and the model calls are billed.
        string? saveFailure = null;
        try
        {
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "The turn of conversation {ConversationId} could not be saved", conversation.Id);
            saveFailure = "La conversation a été modifiée en même temps ailleurs : ce tour n'a pas été enregistré.";
        }

        if (saveFailure is not null)
        {
            yield return new AgentEvent(AgentEvent.Error, new { code = "Conversations.Concurrent", message = saveFailure });
        }

        yield return new AgentEvent(AgentEvent.Done, new { conversationId = conversation.Id });
    }

    private IAgentDefinition? FindAgent(string agentName) =>
        agents.FirstOrDefault(a => string.Equals(a.Name, agentName, StringComparison.OrdinalIgnoreCase));

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
        AgentTool tool,
        AIFunctionArguments arguments,
        Dictionary<string, PendingAction> proposals,
        CancellationToken cancellationToken)
    {
        var action = PendingAction.Propose(
            conversation.Id,
            conversation.UserId,
            tool.Name,
            ChatMessageSerializer.SerializeValue(arguments.ToDictionary(a => a.Key, a => a.Value, StringComparer.Ordinal)),
            tool.RequiredScope,
            dateTimeProvider.UtcNow);

        dbContext.PendingActions.Add(action);
        proposals[FunctionInvokingChatClient.CurrentContext?.CallContent.CallId ?? action.Id.ToString()] = action;

        // Saved now (with the conversation and its first messages): the user can approve it as soon as it is announced.
        await dbContext.SaveChangesAsync(cancellationToken);

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
                        new ToolCallEvent(call.CallId, call.Name, toolCatalog.Find(call.Name)?.IsReadOnly ?? true, call.Arguments));
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
