using Pricely.Modules.AiEngine.Infrastructure.Prices;
using Pricely.Modules.AiEngine.Application.Abstractions.Prices;
using Pricely.Modules.AiEngine.Application;
using Pricely.Modules.AiEngine.Application.Models;
using Pricely.Modules.AiEngine.Application.Usage;
using Pricely.Modules.AiEngine.Infrastructure.Usage;
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
        services.AddScoped<IModelReadStore, ModelReadStore>();
        services.AddScoped<IUsageReadStore, UsageReadStore>();

        // Prices of the models (ADR-021): the list of LiteLLM, else the AI on the pricing page of the provider.
        services.AddSingleton(configuration.GetSection(ModelPriceOptions.SectionName).Get<ModelPriceOptions>() ?? new ModelPriceOptions());
        // One attempt with its own timeout (3 MB): the standard resilience would retry and stop it after 10 s.
        // RemoveAllResilienceHandlers is still an evaluation API of Microsoft.Extensions.Http.Resilience 10.2 (EXTEXP0001).
#pragma warning disable EXTEXP0001
        services.AddHttpClient<IPublishedPriceList, LiteLlmPriceList>(client => client.Timeout = TimeSpan.FromSeconds(60))
            .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
        services.AddAiFeature(AiEngineAiFeatures.PriceReading);

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
