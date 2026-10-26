using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Pricely.Common.Application.Authorization;

namespace Pricely.Common.Presentation.Mcp;

public static class McpServerExtensions
{
    public const string McpRoute = "/mcp";

    private const string AuthorityKey = "Authentication:Authority";

    /// <summary>
    /// MCP server over streamable HTTP, stateless, exposing the tools declared by the modules
    /// (<c>[McpServerToolType]</c> classes; parameters such as <c>ISender</c> are resolved from the request services).
    /// Unauthenticated callers are challenged with the location of the protected resource metadata
    /// (<c>/.well-known/oauth-protected-resource</c>, RFC 9728), which names the authorization server.
    /// </summary>
    public static IServiceCollection AddPricelyMcpServer(this IServiceCollection services, Assembly[] toolAssemblies)
    {
        services
            .AddMcpServer(options => options.ServerInfo = new Implementation { Name = "pricely", Version = "0.1.0" })
            .WithHttpTransport(options => options.Stateless = true);

        // Read-only tools only: write tools need a human approval, which only the internal agent provides (ADR-008),
        // and per-scope filtering of the MCP tools is not in place yet (ADR-013).
        foreach (MethodInfo method in ReadOnlyToolMethods(toolAssemblies))
        {
            services.AddSingleton(serviceProvider => McpServerTool.Create(
                method,
                options: new McpServerToolCreateOptions { Services = serviceProvider }));
        }

        // Tokens are validated by the JWT bearer scheme; the MCP scheme only answers the challenge and serves the metadata.
        services.AddAuthentication(options => options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme)
            .AddMcp(options => options.Events.OnResourceMetadataRequest = context =>
            {
                string? authority = context.HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()[AuthorityKey];

                context.ResourceMetadata = new ProtectedResourceMetadata
                {
                    Resource = $"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{McpRoute}",
                    AuthorizationServers = authority is null ? [] : [authority],
                    ScopesSupported = [.. PricelyScopes.All],
                    BearerMethodsSupported = ["header"]
                };

                return Task.CompletedTask;
            });

        return services;
    }

    /// <summary>
    /// Static methods marked <c>[McpServerTool(ReadOnly = true)]</c> of the <c>[McpServerToolType]</c> classes.
    /// </summary>
    public static IEnumerable<MethodInfo> ReadOnlyToolMethods(IEnumerable<Assembly> toolAssemblies) =>
        ToolMethods(toolAssemblies).Where(method => method.GetCustomAttribute<McpServerToolAttribute>()!.ReadOnly);

    /// <summary>
    /// Static methods marked <c>[McpServerTool]</c> of the <c>[McpServerToolType]</c> classes of the assemblies.
    /// </summary>
    public static IEnumerable<MethodInfo> ToolMethods(IEnumerable<Assembly> toolAssemblies) =>
        toolAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null);

    /// <summary>
    /// Maps the MCP endpoint. Requires the <c>pricely.read</c> scope (ADR-010).
    /// </summary>
    public static IEndpointConventionBuilder MapPricelyMcp(this IEndpointRouteBuilder app) =>
        app.MapMcp(McpRoute).RequireAuthorization(PricelyScopes.Read);
}
