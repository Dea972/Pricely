using Microsoft.Extensions.AI;
using Pricely.Modules.Agentic.Domain.Actions;

namespace Pricely.Modules.Agentic.Application.Conversations;

/// <summary>
/// The decision on an action as the model sees it: a user turn marked as a note (never a system instruction), placed at
/// the time of the decision. Built when the history is replayed, not stored in the conversation: deciding an action
/// never writes the conversation (one aggregate per transaction).
/// </summary>
internal static class ConversationNotes
{
    public static ChatMessage ForDecision(PendingAction action)
    {
        string decision = action.Status switch
        {
            PendingActionStatus.Executed => $"L'utilisateur a validé l'action {action.Id} ({action.ToolName}) ; elle a été exécutée. Résultat : {action.Result}",

            // The error text may come from outside (a merchant page, a provider): it is not passed to the model.
            PendingActionStatus.Failed => $"L'utilisateur a validé l'action {action.Id} ({action.ToolName}), mais elle a échoué.",
            _ => $"L'utilisateur a refusé l'action {action.Id} ({action.ToolName}) : rien n'a été modifié."
        };

        return new ChatMessage(ChatRole.User, $"[Note de l'application, pas un message de l'utilisateur] {decision}");
    }
}
