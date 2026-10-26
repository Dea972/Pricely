using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Agents;

namespace Pricely.Modules.Agentic.Application.Conversations.GetConversations;

internal sealed class GetConversationsQueryHandler(IConversationReadStore readStore, AgentCatalog catalog)
    : IQueryHandler<GetConversationsQuery, IReadOnlyCollection<ConversationSummaryResponse>>
{
    public async Task<Result<IReadOnlyCollection<ConversationSummaryResponse>>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        // Same name matching as the routes of the agents (case-insensitive).
        string? agentName = request.AgentName is null ? null : catalog.FindAgent(request.AgentName)?.Name ?? request.AgentName;

        return Result.Success(
            await readStore.GetSummariesAsync(request.UserId, agentName, GetConversationsQuery.MaxConversations, cancellationToken));
    }
}
