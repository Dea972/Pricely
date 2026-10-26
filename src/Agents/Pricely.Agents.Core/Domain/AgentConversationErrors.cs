using Pricely.Common.Domain;

namespace Pricely.Agents.Core.Domain;

public static class AgentConversationErrors
{
    public static readonly Error EmptyMessage = Error.Problem("Conversations.EmptyMessage", "The message is empty.");

    public static readonly Error MessageTooLong = Error.Problem(
        "Conversations.MessageTooLong",
        $"The message is longer than {AgentConversation.MessageMaxLength} characters.");

    public static Error UnknownAgent(string agentName) =>
        Error.NotFound("Conversations.UnknownAgent", $"There is no agent named '{agentName}'.");

    public static Error NotFound(Guid conversationId) =>
        Error.NotFound("Conversations.NotFound", $"The conversation {conversationId} was not found.");

    public static Error AgentMismatch(Guid conversationId, string agentName) =>
        Error.Problem("Conversations.AgentMismatch", $"The conversation {conversationId} does not belong to the agent '{agentName}'.");
}
