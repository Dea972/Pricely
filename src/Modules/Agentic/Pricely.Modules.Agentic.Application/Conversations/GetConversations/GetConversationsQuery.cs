using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Agentic.Application.Conversations.GetConversations;

/// <summary>
/// The caller's conversations, most recent first, optionally with one agent only.
/// </summary>
public sealed record GetConversationsQuery(Guid UserId, string? AgentName) : IQuery<IReadOnlyCollection<ConversationSummaryResponse>>
{
    public const int MaxConversations = 100;
}

public sealed class ConversationSummaryResponse
{
    public Guid Id { get; init; }

    public string AgentName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public DateTime StartedOnUtc { get; init; }

    public DateTime LastActivityOnUtc { get; init; }
}
