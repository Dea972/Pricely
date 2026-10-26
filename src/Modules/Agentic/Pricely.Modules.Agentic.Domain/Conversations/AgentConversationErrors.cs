using Pricely.Common.Domain;

namespace Pricely.Modules.Agentic.Domain.Conversations;

public static class AgentConversationErrors
{
    public static readonly Error EmptyMessage = Error.Problem("Conversations.EmptyMessage", "The message is empty.");

    public static readonly Error MessageTooLong = Error.Problem(
        "Conversations.MessageTooLong",
        $"The message is longer than {AgentConversation.MessageMaxLength} characters.");

    public static readonly Error UnknownUser = Error.Failure(
        "Conversations.UnknownUser",
        "The caller is not a known user of the application.");

    public static readonly Error ModelFailed = Error.Failure(
        "Conversations.ModelFailed",
        "Le modèle n'a pas pu répondre. Réessaie dans un instant.");

    public static readonly Error ConcurrentTurn = Error.Conflict(
        "Conversations.ConcurrentTurn",
        "La conversation a été modifiée en même temps ailleurs : ce tour n'a pas été enregistré.");

    public static Error UnknownAgent(string agentName) =>
        Error.NotFound("Conversations.UnknownAgent", $"There is no agent named '{agentName}'.");

    public static Error NotFound(Guid conversationId) =>
        Error.NotFound("Conversations.NotFound", $"The conversation {conversationId} was not found.");

    public static Error AgentMismatch(Guid conversationId, string agentName) =>
        Error.Problem("Conversations.AgentMismatch", $"The conversation {conversationId} does not belong to the agent '{agentName}'.");
}
