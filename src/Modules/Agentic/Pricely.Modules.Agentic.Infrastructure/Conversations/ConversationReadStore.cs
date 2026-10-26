using System.Data.Common;
using Dapper;
using Pricely.Common.Infrastructure.Data;
using Pricely.Modules.Agentic.Application.Conversations;
using Pricely.Modules.Agentic.Application.Conversations.GetConversations;

namespace Pricely.Modules.Agentic.Infrastructure.Conversations;

/// <summary>
/// Reads the conversations with SQL (Dapper), kept out of the Application layer.
/// </summary>
internal sealed class ConversationReadStore(IDbConnectionFactory dbConnectionFactory) : IConversationReadStore
{
    public async Task<IReadOnlyCollection<ConversationSummaryResponse>> GetSummariesAsync(
        Guid userId,
        string? agentName,
        int limit,
        CancellationToken cancellationToken = default)
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

        return (await connection.QueryAsync<ConversationSummaryResponse>(
            new CommandDefinition(sql, new { UserId = userId, AgentName = agentName, Limit = limit }, cancellationToken: cancellationToken))).AsList();
    }

    public async Task<ConversationDetails?> GetDetailsAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string conversationSql =
            $"""
             SELECT id AS {nameof(ConversationRow.Id)}, agent_name AS {nameof(ConversationRow.AgentName)},
                    title AS {nameof(ConversationRow.Title)}, started_on_utc AS {nameof(ConversationRow.StartedOnUtc)}
             FROM agentic.conversations
             WHERE id = @ConversationId AND user_id = @UserId
             """;

        const string messagesSql =
            $"""
             SELECT sequence AS {nameof(ConversationMessageRow.Sequence)}, role AS {nameof(ConversationMessageRow.Role)},
                    content AS {nameof(ConversationMessageRow.Content)}, created_on_utc AS {nameof(ConversationMessageRow.CreatedOnUtc)}
             FROM agentic.messages
             WHERE conversation_id = @ConversationId
             ORDER BY sequence
             """;

        const string actionsSql =
            $"""
             SELECT id AS {nameof(ConversationActionRow.Id)}, tool_name AS {nameof(ConversationActionRow.ToolName)},
                    arguments AS {nameof(ConversationActionRow.Arguments)},
                    required_scope AS {nameof(ConversationActionRow.RequiredScope)}, status AS {nameof(ConversationActionRow.Status)},
                    proposed_on_utc AS {nameof(ConversationActionRow.ProposedOnUtc)}, decided_on_utc AS {nameof(ConversationActionRow.DecidedOnUtc)},
                    result AS {nameof(ConversationActionRow.Result)}, error AS {nameof(ConversationActionRow.Error)}
             FROM agentic.pending_actions
             WHERE conversation_id = @ConversationId
             ORDER BY proposed_on_utc
             """;

        var parameters = new { ConversationId = conversationId, UserId = userId };

        ConversationRow? conversation = await connection.QuerySingleOrDefaultAsync<ConversationRow>(
            new CommandDefinition(conversationSql, parameters, cancellationToken: cancellationToken));

        if (conversation is null)
        {
            return null;
        }

        List<ConversationMessageRow> messages = (await connection.QueryAsync<ConversationMessageRow>(
            new CommandDefinition(messagesSql, parameters, cancellationToken: cancellationToken))).AsList();
        List<ConversationActionRow> actions = (await connection.QueryAsync<ConversationActionRow>(
            new CommandDefinition(actionsSql, parameters, cancellationToken: cancellationToken))).AsList();

        return new ConversationDetails(conversation.Id, conversation.AgentName, conversation.Title, conversation.StartedOnUtc, messages, actions);
    }

    internal sealed class ConversationRow
    {
        internal Guid Id { get; init; }

        internal string AgentName { get; init; } = string.Empty;

        internal string Title { get; init; } = string.Empty;

        internal DateTime StartedOnUtc { get; init; }
    }
}
