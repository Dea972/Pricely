using Dapper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricely.Common.Application.Caching;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Web;
using Pricely.Common.Infrastructure.Caching;
using Pricely.Common.Infrastructure.Clock;
using Pricely.Common.Infrastructure.Data;
using Pricely.Common.Infrastructure.EventBus;
using Pricely.Common.Infrastructure.Outbox;
using Pricely.Common.Infrastructure.Web;
using Quartz;

namespace Pricely.Common.Infrastructure;
public static class InfrastructureConfiguration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Action<IRegistrationConfigurator>[] moduleConfigureConsumers)
    {
        SqlMapper.AddTypeHandler(new UtcDateTimeTypeHandler());

        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddDistributedMemoryCache();


        services.TryAddScoped<IDbConnectionFactory>(provider =>
            new DbConnectionFactory(connectionString));
        services.TryAddScoped<IDbConnectionFactory, DbConnectionFactory>();

        services.TryAddSingleton<ICacheService, CacheService>();

        services.TryAddScoped<IEventBus, MassTransitEventBus>();

        services.AddWebPageReader();

        services.AddQuartz(configurator =>
        {
            var scheduler = Guid.NewGuid();
            configurator.SchedulerId = $"default-id-{scheduler}";
            configurator.SchedulerName = $"default-name-{scheduler}";
        });


        services.AddDistributedMemoryCache(); // This requires the Microsoft.Extensions.Caching.Memory namespace


        services.TryAddSingleton<InsertOutboxMessagesInterceptor>();

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        services.AddMassTransit(configure =>
        {
            foreach (Action<IRegistrationConfigurator> configureConsumers in moduleConfigureConsumers)
            {
                configureConsumers(configure);
            }

            configure.SetKebabCaseEndpointNameFormatter();

            configure.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });

        return services;
    }

    /// <summary>
    /// The reader of web pages (merchant, product and pricing pages): one attempt per page, its own guarded connections.
    /// </summary>
    private static void AddWebPageReader(this IServiceCollection services)
    {
        services.TryAddScoped<IWebPageReader, HttpWebPageReader>();

        // RemoveAllResilienceHandlers is still an evaluation API of Microsoft.Extensions.Http.Resilience 10.2 (EXTEXP0001).
#pragma warning disable EXTEXP0001
        services
            .AddHttpClient(HttpWebPageReader.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(HttpWebPageReader.UserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(HttpWebPageReader.CreateHandler)

            // The standard resilience of the service defaults would read a page three times on a failure, with its own
            // 10 s timeout (ADR-017).
            .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
    }
}
