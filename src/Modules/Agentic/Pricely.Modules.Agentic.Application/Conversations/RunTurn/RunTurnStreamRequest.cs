using MediatR;

namespace Pricely.Modules.Agentic.Application.Conversations.RunTurn;

/// <summary>
/// Runs one turn of a started conversation (see <c>StartTurnCommand</c>) and streams what happens.
/// </summary>
public sealed record RunTurnStreamRequest(Guid ConversationId, string Message) : IStreamRequest<AgentEvent>;
