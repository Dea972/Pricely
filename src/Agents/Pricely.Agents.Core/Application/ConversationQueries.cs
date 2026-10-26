using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Pricely.Agents.Core.Domain;
using Pricely.Agents.Core.Infrastructure;
using Pricely.Common.Domain;

namespace Pricely.Agents.Core.Application;

internal sealed class ConversationQueries(
    AgentsDbContext dbContext,
    AgentToolCatalog toolCatalog,
    IEnumerable<IAgentDefinition> agents)
{
    public const int MaxConversations = 100;

    public async Task<IReadOnlyCollection<ConversationSummaryResponse>> ListAsync(
        Guid userId,
        string? agentName,
        CancellationToken cancellationToken)
    {
        // Same name matching as the routes of the agents (case-insensitive).
        string? agent = agentName is null
            ? null
            : agents.FirstOrDefault(a => string.Equals(a.Name, agentName, StringComparison.OrdinalIgnoreCase))?.Name ?? agentName;

        return await dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId && (agent == null || c.AgentName == agent))
            .OrderByDescending(c => c.LastActivityOnUtc)
            .Take(MaxConversations)
            .Select(c => new ConversationSummaryResponse(c.Id, c.AgentName, c.Title, c.StartedOnUtc, c.LastActivityOnUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<ConversationResponse>> GetAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        AgentConversation? conversation = await dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation is null || conversation.EnsureOwnedBy(userId).IsFailure)
        {
            return Result.Failure<ConversationResponse>(AgentConversationErrors.NotFound(conversationId));
        }

        List<PendingAction> actions = await dbContext.PendingActions
            .AsNoTracking()
            .Where(a => a.ConversationId == conversationId)
            .OrderBy(a => a.ProposedOnUtc)
            .ToListAsync(cancellationToken);

        var callNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var messages = new List<ConversationMessageResponse>();

        foreach (AgentMessage message in conversation.Messages.OrderBy(m => m.Sequence))
        {
            if (message.Role == AgentMessageRole.System)
            {
                continue;
            }

            ChatMessage chatMessage = ChatMessageSerializer.Deserialize(message.Content);

            var calls = chatMessage.Contents.OfType<FunctionCallContent>()
                .Select(c =>
                {
                    callNames[c.CallId] = c.Name;
                    return new ToolCallEvent(c.CallId, c.Name, toolCatalog.Find(c.Name)?.IsReadOnly ?? true, c.Arguments);
                })
                .ToList();

            var results = chatMessage.Contents.OfType<FunctionResultContent>()
                .Select(r => new ToolResultEvent(
                    r.CallId,
                    callNames.GetValueOrDefault(r.CallId, string.Empty),
                    r.Result is null ? null : ChatMessageSerializer.ToElement(r.Result),
                    r.Exception?.Message))
                .ToList();

            messages.Add(new ConversationMessageResponse(message.Sequence, message.Role.ToString(), chatMessage.Text, calls, results, message.CreatedOnUtc));
        }

        return new ConversationResponse(
            conversation.Id,
            conversation.AgentName,
            conversation.Title,
            conversation.StartedOnUtc,
            messages,
            actions.Select(PendingActionResponse.From).ToList());
    }
}
