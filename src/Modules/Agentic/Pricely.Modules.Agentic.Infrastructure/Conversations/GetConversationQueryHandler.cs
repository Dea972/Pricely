using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Actions;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Application.Conversations;
using Pricely.Modules.Agentic.Application.Conversations.GetConversation;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Infrastructure.Conversations;

internal sealed class GetConversationQueryHandler(IDbConnectionFactory dbConnectionFactory, AgentCatalog catalog)
    : IQueryHandler<GetConversationQuery, ConversationResponse>
{
    public async Task<Result<ConversationResponse>> Handle(GetConversationQuery request, CancellationToken cancellationToken)
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
             SELECT sequence AS {nameof(MessageRow.Sequence)}, role AS {nameof(MessageRow.Role)},
                    content AS {nameof(MessageRow.Content)}, created_on_utc AS {nameof(MessageRow.CreatedOnUtc)}
             FROM agentic.messages
             WHERE conversation_id = @ConversationId
             ORDER BY sequence
             """;

        const string actionsSql =
            $"""
             SELECT id AS {nameof(ActionRow.Id)}, tool_name AS {nameof(ActionRow.ToolName)}, arguments AS {nameof(ActionRow.Arguments)},
                    required_scope AS {nameof(ActionRow.RequiredScope)}, status AS {nameof(ActionRow.Status)},
                    proposed_on_utc AS {nameof(ActionRow.ProposedOnUtc)}, decided_on_utc AS {nameof(ActionRow.DecidedOnUtc)},
                    result AS {nameof(ActionRow.Result)}, error AS {nameof(ActionRow.Error)}
             FROM agentic.pending_actions
             WHERE conversation_id = @ConversationId
             ORDER BY proposed_on_utc
             """;

        ConversationRow? conversation = await connection.QuerySingleOrDefaultAsync<ConversationRow>(
            new CommandDefinition(conversationSql, request, cancellationToken: cancellationToken));

        if (conversation is null)
        {
            return Result.Failure<ConversationResponse>(AgentConversationErrors.NotFound(request.ConversationId));
        }

        IEnumerable<MessageRow> messages = await connection.QueryAsync<MessageRow>(
            new CommandDefinition(messagesSql, request, cancellationToken: cancellationToken));
        IEnumerable<ActionRow> actions = await connection.QueryAsync<ActionRow>(
            new CommandDefinition(actionsSql, request, cancellationToken: cancellationToken));

        return new ConversationResponse(
            conversation.Id,
            conversation.AgentName,
            conversation.Title,
            conversation.StartedOnUtc,
            ConversationProjection.ToMessages(messages.Select(m => (m.Sequence, m.Role, m.Content, m.CreatedOnUtc)), catalog),
            actions
                .Select(a => new PendingActionResponse(
                    a.Id,
                    conversation.Id,
                    a.ToolName,
                    PendingActionResponse.Parse(a.Arguments)!.Value,
                    a.RequiredScope,
                    a.Status,
                    a.ProposedOnUtc,
                    a.DecidedOnUtc,
                    PendingActionResponse.Parse(a.Result),
                    a.Error))
                .ToList());
    }

    internal sealed class ConversationRow
    {
        internal Guid Id { get; init; }

        internal string AgentName { get; init; } = string.Empty;

        internal string Title { get; init; } = string.Empty;

        internal DateTime StartedOnUtc { get; init; }
    }

    internal sealed class MessageRow
    {
        internal int Sequence { get; init; }

        internal string Role { get; init; } = string.Empty;

        internal string Content { get; init; } = string.Empty;

        internal DateTime CreatedOnUtc { get; init; }
    }

    internal sealed class ActionRow
    {
        internal Guid Id { get; init; }

        internal string ToolName { get; init; } = string.Empty;

        internal string Arguments { get; init; } = string.Empty;

        internal string RequiredScope { get; init; } = string.Empty;

        internal string Status { get; init; } = string.Empty;

        internal DateTime ProposedOnUtc { get; init; }

        internal DateTime? DecidedOnUtc { get; init; }

        internal string? Result { get; init; }

        internal string? Error { get; init; }
    }
}
