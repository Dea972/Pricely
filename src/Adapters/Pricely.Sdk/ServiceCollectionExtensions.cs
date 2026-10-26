using Pricely.Common.Application;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Infrastructure;
using Pricely.Common.Infrastructure.Ai;
using Pricely.Common.Infrastructure.Authentication;
using Pricely.Common.Infrastructure.Authorization;
using Pricely.Common.Infrastructure.Configuration;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Common.Presentation.Mcp;
using Pricely.Agents.Assistant;
using Pricely.Modules.Agentic.Infrastructure;
using Pricely.Modules.AiEngine.Infrastructure;
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
        services.AddApplication(
        [
            .. CatalogModule.HandlerAssemblies,
            .. PricingModule.HandlerAssemblies,
            .. UsersModule.HandlerAssemblies,
            .. AiEngineModule.HandlerAssemblies,
            .. AgenticModule.HandlerAssemblies
        ]);

        // Register infrastructure concerns (DbContext, repositories, outbox, message bus, etc.).
        services.AddInfrastructure(connectionString, [PricingModule.ConfigureConsumers, AiEngineModule.ConfigureConsumers]);

        // Register the LLM providers and the gateway (every LLM call goes through ILlmGateway, ADR-007).
        services.AddAiProviders(configuration);

        // Register authentication (JWT access tokens issued by Keycloak) and authorization (scope and permission policies).
        services.AddAuthenticationInternal();
        services.AddAuthorizationInternal();

        // Users module: profiles, roles and business permissions (identity is held by Keycloak, ADR-010).
        services.AddUsersModule(configuration, connectionString);

        // Catalog module: reference products and product typologies.
        services.AddCatalogModule(configuration, connectionString);

        // Pricing module: merchants, collection profiles, offers and price observations.
        services.AddPricingModule(configuration, connectionString);

        // AiEngine module: model catalog and prices, target of each AI feature, LLM call journal and usage.
        services.AddAiEngineModule(configuration, connectionString);

        // Keep this section as the single place to aggregate module additions.


        // MCP server over HTTP (/mcp): the read-only tools declared by the modules (ADR-009).
        System.Reflection.Assembly[] toolAssemblies = [CatalogModule.ToolAssembly, PricingModule.ToolAssembly];
        services.AddPricelyMcpServer(toolAssemblies);

        // Agentic module (ADR-014): runs the declared agents with the same tools, write tools included (behind approval).
        services.AddAgenticModule(configuration, connectionString);
        services.AddAgentTools(McpServerExtensions.ToAgentTools(toolAssemblies));
        services.AddAssistantAgent();

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

        // MCP server (streamable HTTP), outside /api, protected like the API.
        app.MapPricelyMcp();

        // Return the application builder for further middleware chaining.
        return app;
    }
}
