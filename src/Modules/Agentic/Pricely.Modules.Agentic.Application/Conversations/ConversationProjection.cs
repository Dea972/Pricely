using Microsoft.Extensions.AI;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Application.Conversations.GetConversation;
using Pricely.Modules.Agentic.Application.Conversations.RunTurn;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Application.Conversations;

/// <summary>
/// Turns the stored model messages into the messages shown to the user (used by the read side).
/// </summary>
public static class ConversationProjection
{
    public static IReadOnlyCollection<ConversationMessageResponse> ToMessages(
        IEnumerable<(int Sequence, string Role, string Content, DateTime CreatedOnUtc)> messages,
        AgentCatalog catalog)
    {
        var callNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var result = new List<ConversationMessageResponse>();

        foreach ((int sequence, string role, string content, DateTime createdOnUtc) in messages.OrderBy(m => m.Sequence))
        {
            if (string.Equals(role, nameof(AgentMessageRole.System), StringComparison.Ordinal))
            {
                continue;
            }

            ChatMessage chatMessage = ChatMessageSerializer.Deserialize(content);

            var calls = chatMessage.Contents.OfType<FunctionCallContent>()
                .Select(c =>
                {
                    callNames[c.CallId] = c.Name;
                    return new ToolCallEvent(c.CallId, c.Name, catalog.FindTool(c.Name)?.IsReadOnly ?? true, c.Arguments);
                })
                .ToList();

            var results = chatMessage.Contents.OfType<FunctionResultContent>()
                .Select(r => new ToolResultEvent(
                    r.CallId,
                    callNames.GetValueOrDefault(r.CallId, string.Empty),
                    r.Result is null ? null : ChatMessageSerializer.ToElement(r.Result),
                    r.Exception?.Message))
                .ToList();

            result.Add(new ConversationMessageResponse(sequence, role, chatMessage.Text, calls, results, createdOnUtc));
        }

        return result;
    }
}
