using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Infrastructure.Outbox;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Catalog.Application;
using Pricely.Modules.Catalog.Application.Abstractions.Data;
using Pricely.Modules.Catalog.Contracts.Categories;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Domain.Products;
using Pricely.Modules.Catalog.Infrastructure.Categories;
using Pricely.Modules.Catalog.Infrastructure.Contracts.Categories;
using Pricely.Modules.Catalog.Infrastructure.Database;
using Pricely.Modules.Catalog.Infrastructure.Outbox;
using Pricely.Modules.Catalog.Infrastructure.Products;

namespace Pricely.Modules.Catalog.Infrastructure;

public static class CatalogModule
{
    /// <summary>
    /// Assemblies containing the module's MediatR handlers and validators
    /// (command handlers in Application, query handlers in Infrastructure).
    /// </summary>
    public static readonly System.Reflection.Assembly[] HandlerAssemblies =
    [
        Application.AssemblyReference.Assembly,
        typeof(CatalogModule).Assembly
    ];

    /// <summary>
    /// Assembly declaring the module's MCP / agent tools (<c>[McpServerToolType]</c> classes in Presentation).
    /// </summary>
    public static readonly System.Reflection.Assembly ToolAssembly = Presentation.AssemblyReference.Assembly;

    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(
                    connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                        HistoryRepository.DefaultTableName,
                        Schemas.Catalog))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<CatalogDbContext>());
        services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Public read surface for the other modules (ADR-022).
        services.AddScoped<IProductCategoryReader, ProductCategoryReader>();

        services.AddAiFeature(CatalogAiFeatures.ProductEntry);

        services.AddDomainEventHandlers();

        services.AddEndpoints(Presentation.AssemblyReference.Assembly);

        services.Configure<OutboxOptions>(configuration.GetSection("Catalog:Outbox"));
        services.ConfigureOptions<ConfigureProcessOutboxJob>();

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
