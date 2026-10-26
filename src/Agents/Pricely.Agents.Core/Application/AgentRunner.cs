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
    ILoggerFactory loggerFactory)
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
                tools.Add(tool.IsReadOnly ? tool.Function : new ProposalFunction(tool, (t, arguments) => Propose(conversation, t, arguments, proposals)));
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
                    failure = exception.Message;
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

        foreach (ChatMessage produced in updates.ToChatResponse().Messages)
        {
            conversation.Append(ChatMessageSerializer.ToRole(produced.Role), ChatMessageSerializer.Serialize(produced), dateTimeProvider.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        yield return new AgentEvent(AgentEvent.Done, new { conversationId = conversation.Id });
    }

    private IAgentDefinition? FindAgent(string agentName) =>
        agents.FirstOrDefault(a => string.Equals(a.Name, agentName, StringComparison.OrdinalIgnoreCase));

    private PendingAction Propose(
        AgentConversation conversation,
        AgentTool tool,
        AIFunctionArguments arguments,
        Dictionary<string, PendingAction> proposals)
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
