using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Infrastructure.Outbox;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Agentic.Application;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Application.Tools;
using Pricely.Modules.Agentic.Domain.Actions;
using Pricely.Modules.Agentic.Domain.Conversations;
using Pricely.Modules.Agentic.Infrastructure.Actions;
using Pricely.Modules.Agentic.Infrastructure.Conversations;
using Pricely.Modules.Agentic.Infrastructure.Database;
using Pricely.Modules.Agentic.Infrastructure.Outbox;
using Pricely.Modules.Agentic.Application.Conversations;

namespace Pricely.Modules.Agentic.Infrastructure;

/// <summary>
/// Agentic: runs the agents declared by the application (<c>AddAgent</c>) with the tools it gives (<c>AddAgentTools</c>),
/// keeps their conversations and asks a person to approve every write tool (ADR-008, ADR-014). Knows no other module.
/// </summary>
public static class AgenticModule
{
    /// <summary>
    /// Assemblies containing the module's MediatR handlers and validators
    /// (command handlers in Application, query handlers in Infrastructure).
    /// </summary>
    public static readonly System.Reflection.Assembly[] HandlerAssemblies =
    [
        Application.AssemblyReference.Assembly,
        typeof(AgenticModule).Assembly
    ];

    public static IServiceCollection AddAgenticModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddDbContext<AgenticDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(
                    connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Agentic))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<AgenticDbContext>());

        services.AddAgenticCore(configuration.GetSection(AgenticOptions.SectionName).Get<AgenticOptions>() ?? new AgenticOptions());

        services.AddDomainEventHandlers();

        services.AddEndpoints(Presentation.AssemblyReference.Assembly);

        services.Configure<OutboxOptions>(configuration.GetSection("Agentic:Outbox"));
        services.ConfigureOptions<ConfigureProcessOutboxJob>();

        return services;
    }

    /// <summary>Services of the module that do not depend on the database provider (also used by the tests).</summary>
    internal static IServiceCollection AddAgenticCore(this IServiceCollection services, AgenticOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<AgentCatalog>();
        services.AddScoped<ToolInvocationJournal>();
        services.AddScoped<IAgentConversationRepository, AgentConversationRepository>();
        services.AddScoped<IPendingActionRepository, PendingActionRepository>();
        services.AddScoped<IConversationReadStore, ConversationReadStore>();

        return services;
    }

    private static void AddDomainEventHandlers(this IServiceCollection services)
    {
        Type[] domainEventHandlers = Application.AssemblyReference.Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IDomainEventHandler)))
            .ToArray();

        foreach (Type domainEventHandler in domainEventHandlers)
        {
            services.TryAddScoped(domainEventHandler);

            Type domainEvent = domainEventHandler
                .GetInterfaces()
                .Single(i => i.IsGenericType)
                .GetGenericArguments()
                .Single();

            Type closedIdempotentHandler = typeof(IdempotentDomainEventHandler<>).MakeGenericType(domainEvent);

            services.Decorate(domainEventHandler, closedIdempotentHandler);
        }
    }
}
