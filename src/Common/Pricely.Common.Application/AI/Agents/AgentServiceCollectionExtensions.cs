using Microsoft.Extensions.DependencyInjection;

namespace Pricely.Common.Application.AI.Agents;

public static class AgentServiceCollectionExtensions
{
    /// <summary>
    /// Declares an agent and its AI feature. The Agentic module runs the declared agents.
    /// </summary>
    public static IServiceCollection AddAgent(this IServiceCollection services, AgentDefinition agent) =>
        services.AddSingleton(agent).AddAiFeature(agent.Feature);

    /// <summary>
    /// Gives tools to the agents (each agent only uses the ones it names).
    /// </summary>
    public static IServiceCollection AddAgentTools(this IServiceCollection services, IEnumerable<AgentTool> tools)
    {
        foreach (AgentTool tool in tools)
        {
            services.AddSingleton(tool);
        }

        return services;
    }
}
