using Microsoft.Extensions.DependencyInjection;
using Pricely.Agents.Core;

namespace Pricely.Agents.Assistant;

public static class AssistantAgentRegistration
{
    public static IServiceCollection AddAssistantAgent(this IServiceCollection services) =>
        services.AddAgent<AssistantAgent>();
}
