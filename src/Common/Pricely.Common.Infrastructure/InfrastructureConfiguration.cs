using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricely.Common.Application.Caching;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Clock;
using Pricely.Common.Infrastructure.Caching;
using Pricely.Common.Infrastructure.Clock;
using Pricely.Common.Infrastructure.Data;
using Pricely.Common.Infrastructure.Outbox;
using Quartz;

namespace Pricely.Common.Infrastructure;
public static class InfrastructureConfiguration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Action<IRegistrationConfigurator>[] moduleConfigureConsumers)
    {
        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddDistributedMemoryCache();


        services.TryAddScoped<IDbConnectionFactory>(provider =>
            new DbConnectionFactory(connectionString));
        services.TryAddScoped<IDbConnectionFactory, DbConnectionFactory>();

        services.TryAddSingleton<ICacheService, CacheService>();

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
}
