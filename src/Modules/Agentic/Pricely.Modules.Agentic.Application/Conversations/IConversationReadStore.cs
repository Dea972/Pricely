using Pricely.Modules.Agentic.Application.Conversations.GetConversations;

namespace Pricely.Modules.Agentic.Application.Conversations;

/// <summary>
/// Read side of the conversations (queries), implemented in Infrastructure.
/// </summary>
public interface IConversationReadStore
{
    /// <summary>The user's conversations, most recent activity first, optionally with one agent only.</summary>
    Task<IReadOnlyCollection<ConversationSummaryResponse>> GetSummariesAsync(
        Guid userId,
        string? agentName,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>The conversation of this user with its messages and actions; null when it is not theirs or unknown.</summary>
    Task<ConversationDetails?> GetDetailsAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
}

/// <summary>A conversation as stored: the raw rows the query handler turns into a response.</summary>
public sealed record ConversationDetails(
    Guid Id,
    string AgentName,
    string Title,
    DateTime StartedOnUtc,
    IReadOnlyList<ConversationMessageRow> Messages,
    IReadOnlyList<ConversationActionRow> Actions);

public sealed record ConversationMessageRow(int Sequence, string Role, string Content, DateTime CreatedOnUtc);

public sealed record ConversationActionRow(
    Guid Id,
    string ToolName,
    string Arguments,
    string RequiredScope,
    string Status,
    DateTime ProposedOnUtc,
    DateTime? DecidedOnUtc,
    string? Result,
    string? Error);
