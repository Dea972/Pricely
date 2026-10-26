namespace Pricely.Agents.Core.Domain;

/// <summary>
/// One message of a conversation as exchanged with the model (text, tool calls or tool results), serialized by the
/// application layer so that the conversation can be replayed to the model.
/// </summary>
public sealed class AgentMessage
{
    private AgentMessage()
    {
    }

    public Guid Id { get; private set; }

    public Guid ConversationId { get; private set; }

    public int Sequence { get; private set; }

    public AgentMessageRole Role { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime CreatedOnUtc { get; private set; }

    internal static AgentMessage Create(Guid conversationId, int sequence, AgentMessageRole role, string content, DateTime utcNow) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ConversationId = conversationId,
            Sequence = sequence,
            Role = role,
            Content = content,
            CreatedOnUtc = utcNow
        };
}
