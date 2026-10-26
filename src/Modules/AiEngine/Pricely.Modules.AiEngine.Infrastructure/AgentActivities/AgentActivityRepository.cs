using Pricely.Modules.AiEngine.Domain.AgentActivities;
using Pricely.Modules.AiEngine.Infrastructure.Database;

namespace Pricely.Modules.AiEngine.Infrastructure.AgentActivities;

internal sealed class AgentActivityRepository(AiEngineDbContext context) : IAgentActivityRepository
{
    public void Insert(AgentActivity activity) => context.AgentActivities.Add(activity);
}
