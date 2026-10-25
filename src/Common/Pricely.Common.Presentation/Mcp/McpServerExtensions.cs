using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace Pricely.Common.Presentation.Mcp;

public static class McpServerExtensions
{
    public const string McpRoute = "/mcp";

    /// <summary>
    /// MCP server over streamable HTTP, stateless, exposing the tools declared by the modules
    /// (<c>[McpServerToolType]</c> classes; parameters such as <c>ISender</c> are resolved from the request services).
    /// </summary>
    public static IServiceCollection AddPricelyMcpServer(this IServiceCollection services, Assembly[] toolAssemblies)
    {
        IMcpServerBuilder builder = services
            .AddMcpServer(options => options.ServerInfo = new Implementation { Name = "pricely", Version = "0.1.0" })
            .WithHttpTransport(options => options.Stateless = true);

        foreach (Assembly toolAssembly in toolAssemblies)
        {
            builder.WithToolsFromAssembly(toolAssembly);
        }

        return services;
    }

    /// <summary>
    /// Maps the MCP endpoint. Requires an authenticated caller, like the API (ADR-010).
    /// </summary>
    public static IEndpointConventionBuilder MapPricelyMcp(this IEndpointRouteBuilder app) =>
        app.MapMcp(McpRoute).RequireAuthorization();
}
