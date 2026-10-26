using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Agents.Core.Application;
using Pricely.Agents.Core.Infrastructure;
using Pricely.Common.Application.AI;
using Pricely.Common.Presentation.Endpoints;

namespace Pricely.Agents.Core;

public static class AgentsModule
{
    /// <summary>
    /// Registers the agents foundation: conversations and pending actions (<c>agents</c> schema), the tools of the
    /// modules (given by the composition root, the agents reference no module) and the endpoints under <c>agents/</c>.
    /// </summary>
    public static IServiceCollection AddAgents(this IServiceCollection services, string connectionString, Assembly[] toolAssemblies)
    {
        services.AddDbContext<AgentsDbContext>(options =>
            options
                .UseNpgsql(
                    connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, AgentsDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services.AddSingleton(new AgentToolCatalog(toolAssemblies));
        services.AddScoped<AgentRunner>();
        services.AddScoped<AgentActionService>();
        services.AddScoped<ConversationQueries>();

        services.AddEndpoints(typeof(AgentsModule).Assembly);

        return services;
    }

    /// <summary>
    /// Registers an agent (one project per agent under <c>src/Agents</c>) and its AI feature.
    /// </summary>
    public static IServiceCollection AddAgent<TAgent>(this IServiceCollection services)
        where TAgent : class, IAgentDefinition, new()
    {
        var agent = new TAgent();

        services.AddSingleton<IAgentDefinition>(agent);
        services.AddAiFeature(agent.Feature);

        return services;
    }
}
