using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Infrastructure.Outbox;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Application.CollectionProfiles;
using Pricely.Modules.Pricing.Application.Merchants;
using Pricely.Modules.Pricing.Application.Offers;
using Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;
using Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;
using Pricely.Modules.Pricing.Application.PageSnapshots;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.Domain.PageSnapshots;
using Pricely.Modules.Pricing.Infrastructure.Collection;
using Pricely.Modules.Pricing.Infrastructure.CollectionProfiles;
using Pricely.Modules.Pricing.Infrastructure.Database;
using Pricely.Modules.Pricing.Infrastructure.Inbox;
using Pricely.Modules.Pricing.Infrastructure.Merchants;
using Pricely.Modules.Pricing.Infrastructure.Offers;
using Pricely.Modules.Pricing.Infrastructure.Outbox;
using Pricely.Modules.Pricing.Infrastructure.PageSnapshots;

namespace Pricely.Modules.Pricing.Infrastructure;

public static class PricingModule
{
    /// <summary>
    /// Assemblies containing the module's MediatR handlers and validators
    /// (command handlers in Application, query handlers in Infrastructure).
    /// </summary>
    public static readonly Assembly[] HandlerAssemblies =
    [
        Application.AssemblyReference.Assembly,
        typeof(PricingModule).Assembly
    ];

    /// <summary>
    /// Assembly declaring the module's MCP / agent tools (<c>[McpServerToolType]</c> classes in Presentation).
    /// </summary>
    public static readonly Assembly ToolAssembly = Presentation.AssemblyReference.Assembly;

    public static IServiceCollection AddPricingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddDbContext<PricingDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(
                    connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                        HistoryRepository.DefaultTableName,
                        Schemas.Pricing))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<PricingDbContext>());
        services.AddScoped<IMerchantRepository, MerchantRepository>();
        services.AddScoped<ICollectionProfileRepository, CollectionProfileRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IPriceObservationRepository, PriceObservationRepository>();
        services.AddScoped<IMerchantReadStore, MerchantReadStore>();
        services.AddScoped<ICollectionProfileReadStore, CollectionProfileReadStore>();
        services.AddScoped<IOfferReadStore, OfferReadStore>();

        services.AddScoped<IPageSnapshotRepository, PageSnapshotRepository>();

        services.AddScoped<OfferCollectionPlanner>();
        services.AddScoped<CollectedOfferRecorder>();
        services.AddScoped<MerchantPageReader>();
        services.AddScoped<SubmittedOfferVerifier>();

        // The collection agent (ADR-017); dedicated scrapers or partner feeds can replace it.
        services.TryAddScoped<IOfferCollector, AgentOfferCollector>();

        services.AddAiFeature(PricingAiFeatures.OfferCollection);
        services.AddAiFeature(PricingAiFeatures.MerchantSetup);

        services.AddDomainEventHandlers();
        services.AddIntegrationEventHandlers();

        services.AddEndpoints(Presentation.AssemblyReference.Assembly);

        services.Configure<OutboxOptions>(configuration.GetSection("Pricing:Outbox"));
        services.ConfigureOptions<ConfigureProcessOutboxJob>();

        services.Configure<InboxOptions>(configuration.GetSection("Pricing:Inbox"));
        services.ConfigureOptions<ConfigureProcessInboxJob>();

        services.Configure<CollectionOptions>(configuration.GetSection("Pricing:Collection"));
        services.ConfigureOptions<ConfigureCollectOffersJob>();

        return services;
    }

    /// <summary>
    /// Integration events consumed by the module (stored in its inbox, then handled by Presentation handlers).
    /// </summary>
    public static void ConfigureConsumers(IRegistrationConfigurator registrationConfigurator)
    {
        // No consumer for now: Pricing reads Catalog through its contracts (ADR-022). A future consumer registers
        // IntegrationEventConsumer<TEvent> here.
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
