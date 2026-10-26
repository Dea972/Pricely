namespace Pricely.Modules.Agentic.Domain.Conversations;

public interface IAgentConversationRepository
{
    /// <summary>The conversation with its messages, in order.</summary>
    Task<AgentConversation?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(AgentConversation conversation);
}
