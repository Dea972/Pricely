using Microsoft.Extensions.DependencyInjection;
using Pricely.Common.Application.AI.Agents;

namespace Pricely.Agents.Assistant;

public static class AssistantAgentRegistration
{
    public static IServiceCollection AddAssistantAgent(this IServiceCollection services) =>
        services.AddAgent(AssistantAgent.Definition);
}
