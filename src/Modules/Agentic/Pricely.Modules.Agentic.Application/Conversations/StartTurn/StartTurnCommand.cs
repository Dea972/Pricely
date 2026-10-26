using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Agentic.Application.Conversations.StartTurn;

/// <summary>
/// Checks a user message for an agent and starts the conversation if <see cref="ConversationId"/> is null. The turn
/// itself runs with <c>RunTurnStreamRequest</c>.
/// </summary>
public sealed record StartTurnCommand(string AgentName, Guid? ConversationId, string Message) : ICommand<TurnStartedResponse>;

public sealed record TurnStartedResponse(Guid ConversationId, string Title);
