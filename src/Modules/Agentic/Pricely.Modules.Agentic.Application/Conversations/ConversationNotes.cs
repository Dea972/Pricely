using Microsoft.Extensions.AI;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Application.Conversations;

internal static class ConversationNotes
{
    /// <summary>
    /// A note for the model (for example the decision on an action), kept in its place in the conversation as a user
    /// turn marked as a note (never a system instruction), and hidden from the conversation shown to the user.
    /// </summary>
    public static void AppendNote(this AgentConversation conversation, string note, DateTime utcNow) =>
        conversation.Append(
            AgentMessageRole.System,
            ChatMessageSerializer.Serialize(new ChatMessage(ChatRole.User, $"[Note de l'application, pas un message de l'utilisateur] {note}")),
            utcNow);
}
