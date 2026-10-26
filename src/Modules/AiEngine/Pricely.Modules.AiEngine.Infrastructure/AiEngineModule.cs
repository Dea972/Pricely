using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.Features;
using Pricely.Modules.AiEngine.Domain.AgentActivities;
using Pricely.Modules.AiEngine.Domain.LlmCalls;
using Pricely.Modules.AiEngine.Domain.Models;
using Pricely.Modules.AiEngine.Infrastructure.Database;
using Pricely.Modules.AiEngine.Infrastructure.Features;
using Pricely.Modules.AiEngine.Infrastructure.Inbox;
using Pricely.Modules.AiEngine.Infrastructure.AgentActivities;
using Pricely.Modules.AiEngine.Infrastructure.LlmCalls;
using Pricely.Modules.AiEngine.Infrastructure.Models;

namespace Pricely.Modules.AiEngine.Infrastructure;

/// <summary>
/// AiEngine pilots and observes LLM usage: model catalog and prices, target of each AI feature, call journal and
/// costs. It does not call LLMs for other modules: they go through <see cref="ILlmGateway"/> (Common).
/// </summary>
public static class AiEngineModule
{
    /// <summary>
    /// Assemblies containing the module's MediatR handlers and validators
    /// (command handlers in Application, query handlers in Infrastructure).
    /// </summary>
    public static readonly Assembly[] HandlerAssemblies =
    [
        Application.AssemblyReference.Assembly,
        typeof(AiEngineModule).Assembly
    ];

    public static IServiceCollection AddAiEngineModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddDbContext<AiEngineDbContext>(options =>
            options
                .UseNpgsql(
                    connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                        HistoryRepository.DefaultTableName,
                        Schemas.AiEngine))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<AiEngineDbContext>());
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<IFeatureSettingRepository, FeatureSettingRepository>();
        services.AddScoped<ILlmCallRepository, LlmCallRepository>();
        services.AddScoped<IAgentActivityRepository, AgentActivityRepository>();

        // Replaces the default resolver of Common: the target of a feature comes from the settings.
        services.Replace(ServiceDescriptor.Scoped<ILlmTargetResolver, LlmTargetResolver>());

        services.AddIntegrationEventHandlers();

        services.AddEndpoints(Presentation.AssemblyReference.Assembly);

        services.Configure<InboxOptions>(configuration.GetSection("AiEngine:Inbox"));
        services.ConfigureOptions<ConfigureProcessInboxJob>();

        return services;
    }

    /// <summary>
    /// Integration events consumed by the module (stored in its inbox, then handled by Presentation handlers).
    /// </summary>
    public static void ConfigureConsumers(IRegistrationConfigurator registrationConfigurator)
    {
        registrationConfigurator.AddConsumer<IntegrationEventConsumer<LlmCalledIntegrationEvent>>();
        registrationConfigurator.AddConsumer<IntegrationEventConsumer<AiToolInvokedIntegrationEvent>>();
        registrationConfigurator.AddConsumer<IntegrationEventConsumer<AiActionDecidedIntegrationEvent>>();
    }

    private static void AddIntegrationEventHandlers(this IServiceCollection services)
    {
        Type[] integrationEventHandlers = Presentation.AssemblyReference.Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IIntegrationEventHandler)))
            .ToArray();

        foreach (Type integrationEventHandler in integrationEventHandlers)
        {
            services.TryAddScoped(integrationEventHandler);

            Type integrationEvent = integrationEventHandler
                .GetInterfaces()
                .Single(i => i.IsGenericType)
                .GetGenericArguments()
                .Single();

            Type closedIdempotentHandler =
                typeof(IdempotentIntegrationEventHandler<>).MakeGenericType(integrationEvent);

            services.Decorate(integrationEventHandler, closedIdempotentHandler);
        }
    }
}
