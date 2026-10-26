using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Agents;

namespace Pricely.Modules.Agentic.Application.Conversations.GetConversations;

internal sealed class GetConversationsQueryHandler(IDbConnectionFactory dbConnectionFactory, AgentCatalog catalog)
    : IQueryHandler<GetConversationsQuery, IReadOnlyCollection<ConversationSummaryResponse>>
{
    public async Task<Result<IReadOnlyCollection<ConversationSummaryResponse>>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(ConversationSummaryResponse.Id)},
                 agent_name AS {nameof(ConversationSummaryResponse.AgentName)},
                 title AS {nameof(ConversationSummaryResponse.Title)},
                 started_on_utc AS {nameof(ConversationSummaryResponse.StartedOnUtc)},
                 last_activity_on_utc AS {nameof(ConversationSummaryResponse.LastActivityOnUtc)}
             FROM agentic.conversations
             WHERE user_id = @UserId
               AND (CAST(@AgentName AS text) IS NULL OR agent_name = @AgentName)
             ORDER BY last_activity_on_utc DESC
             LIMIT @Limit
             """;

        // Same name matching as the routes of the agents (case-insensitive).
        string? agentName = request.AgentName is null ? null : catalog.FindAgent(request.AgentName)?.Name ?? request.AgentName;

        IEnumerable<ConversationSummaryResponse> conversations = await connection.QueryAsync<ConversationSummaryResponse>(
            new CommandDefinition(
                sql,
                new { request.UserId, AgentName = agentName, Limit = GetConversationsQuery.MaxConversations },
                cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<ConversationSummaryResponse>>(conversations.ToList());
    }
}
