using Pricely.Common.Application;
using Pricely.Common.Infrastructure;
using Pricely.Common.Infrastructure.Authentication;
using Pricely.Common.Infrastructure.Authorization;
using Pricely.Common.Infrastructure.Configuration;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Common.Presentation.Tools;
using Pricely.Modules.Catalog.Infrastructure;
using Pricely.Modules.Pricing.Infrastructure;
using Pricely.Modules.Users.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pricely.Infrastructure;

/// <summary>
/// Provides extension methods to bootstrap the Modular Monolith Template.
/// Centralizes registration of application, infrastructure, tools, endpoints and modules.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all core services required by the modular monolith.
    /// Add your module-specific registration calls in the indicated section.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="connectionString">The database or persistence connection string.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The same IServiceCollection for fluent chaining.</returns>
    /// <remarks>
    /// Currently this method does not perform any awaited operations.
    /// The async signature is kept in case future module initialization requires asynchronous calls.
    /// </remarks>
    public async static Task<IServiceCollection> AddPricelyAsync(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        // Register application layer services (CQRS handlers, mediators, validators, etc.).
        services.AddApplication([.. CatalogModule.HandlerAssemblies, .. PricingModule.HandlerAssemblies]);

        // Register infrastructure concerns (DbContext, repositories, outbox, message bus, etc.).
        services.AddInfrastructure(connectionString, [PricingModule.ConfigureConsumers]);

        // Register authentication (JWT Bearer) and authorization (permission-based policies).
        await services.AddAuthenticationInternalAsync(configuration);
        services.AddAuthorizationInternal();

        // Register Users module (authentication, authorization, user management).
        await services.AddUsersModuleAsync(configuration);

        // Catalog module: reference products and product typologies.
        services.AddCatalogModule(configuration, connectionString);

        // Pricing module: merchants, collection profiles, offers and price observations.
        services.AddPricingModule(configuration, connectionString);

        // Keep this section as the single place to aggregate module additions.


        // Register MCP server (Model Context Protocol) with tool implementations.
        services.AddMcpServerWithTools([]);

        // Discover and register endpoint definitions from presentation layer / modules.
        services.AddEndpoints((System.Reflection.Assembly[])Array.Empty<object>());

        // Return the modified service collection.
        return services;
    }

    /// <summary>
    /// Maps all collected endpoints under the /api route group.
    /// Call this in Program.cs after building the WebApplication.
    /// </summary>
    /// <param name="app">The current WebApplication instance.</param>
    /// <returns>The same IApplicationBuilder for fluent chaining.</returns>
    public static IApplicationBuilder MapPricelyEndpoints(this WebApplication app)
    {
        // Create a route group for version-neutral API endpoints.
        RouteGroupBuilder apiGroup = app.MapGroup("/api");

        // Delegate mapping to the endpoint discovery/registration mechanism.
        app.MapEndpoints(apiGroup);

        // Return the application builder for further middleware chaining.
        return app;
    }
}
