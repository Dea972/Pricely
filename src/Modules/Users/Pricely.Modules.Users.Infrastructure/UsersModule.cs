using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Infrastructure.Outbox;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Users.Application.Abstractions.Data;
using Pricely.Modules.Users.Application.Users;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.Infrastructure.Authorization;
using Pricely.Modules.Users.Infrastructure.Database;
using Pricely.Modules.Users.Infrastructure.Inbox;
using Pricely.Modules.Users.Infrastructure.Outbox;
using Pricely.Modules.Users.Infrastructure.Users;

namespace Pricely.Modules.Users.Infrastructure;

public static class UsersModule
{
    /// <summary>
    /// Assemblies containing the module's MediatR handlers and validators
    /// (command handlers in Application, query handlers in Infrastructure).
    /// </summary>
    public static readonly System.Reflection.Assembly[] HandlerAssemblies =
    [
        Application.AssemblyReference.Assembly,
        typeof(UsersModule).Assembly
    ];

    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddDomainEventHandlers();

        services.AddIntegrationEventHandlers();

        services.AddInfrastructure(connectionString);

        services.AddEndpoints(Presentation.AssemblyReference.Assembly);

        services.Configure<OutboxOptions>(configuration.GetSection("Users:Outbox"));

        services.ConfigureOptions<ConfigureProcessOutboxJob>();

        return services;
    }

    private static void AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<UsersDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(
                    connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                        HistoryRepository.DefaultTableName,
                        Schemas.Users))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserReadStore, UserReadStore>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UsersDbContext>());

        services.AddScoped<IPermissionService, PermissionService>();
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
