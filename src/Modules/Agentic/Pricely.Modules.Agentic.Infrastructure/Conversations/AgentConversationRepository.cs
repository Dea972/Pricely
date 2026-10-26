using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Agentic.Domain.Conversations;
using Pricely.Modules.Agentic.Infrastructure.Database;

namespace Pricely.Modules.Agentic.Infrastructure.Conversations;

internal sealed class AgentConversationRepository(AgenticDbContext context) : IAgentConversationRepository
{
    public Task<AgentConversation?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Conversations.Include(c => c.Messages).SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Insert(AgentConversation conversation) => context.Conversations.Add(conversation);
}
