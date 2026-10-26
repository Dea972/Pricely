using Pricely.Common.Application.Messaging;
using Pricely.Modules.Agentic.Application.Actions;
using Pricely.Modules.Agentic.Application.Conversations.RunTurn;

namespace Pricely.Modules.Agentic.Application.Conversations.GetConversation;

/// <summary>
/// A conversation of the caller as shown to them: messages with the tools called and their typed results, and the actions.
/// </summary>
public sealed record GetConversationQuery(Guid ConversationId, Guid UserId) : IQuery<ConversationResponse>;

public sealed record ConversationResponse(
    Guid Id,
    string AgentName,
    string Title,
    DateTime StartedOnUtc,
    IReadOnlyCollection<ConversationMessageResponse> Messages,
    IReadOnlyCollection<PendingActionResponse> Actions);

/// <summary>
/// A message as shown to the user. Notes for the model (decisions on actions) are not included: the actions carry their status.
/// </summary>
public sealed record ConversationMessageResponse(
    int Sequence,
    string Role,
    string Text,
    IReadOnlyCollection<ToolCallEvent> ToolCalls,
    IReadOnlyCollection<ToolResultEvent> ToolResults,
    DateTime CreatedOnUtc);
