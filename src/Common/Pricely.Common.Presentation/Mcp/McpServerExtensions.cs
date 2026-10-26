using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Pricely.Common.Application.AI.Agents;
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
        // The scope each tool needs (ADR-016): read tools pricely.read, write tools their [ToolScope] or pricely.write.
        var requiredScopes = ToolMethods(toolAssemblies).ToDictionary(ToolName, RequiredScope, StringComparer.Ordinal);

        IMcpServerBuilder mcp = services
            .AddMcpServer(options => options.ServerInfo = new Implementation { Name = "pricely", Version = "0.1.0" })
            .WithHttpTransport(options => options.Stateless = true)
            .WithRequestFilters(filters => filters.AddListToolsFilter(next => async (context, cancellationToken) =>
            {
                // A client only sees the tools its token may call.
                ListToolsResult result = await next(context, cancellationToken);
                IReadOnlySet<string> scopes = ScopesOf(context.Services);
                result.Tools = [.. result.Tools.Where(tool => scopes.Contains(requiredScopes.GetValueOrDefault(tool.Name, PricelyScopes.Write)))];

                return result;
            }));

        foreach (Assembly assembly in toolAssemblies)
        {
            mcp.WithPromptsFromAssembly(assembly);
        }

        // Every tool, the write ones included: the MCP client asks its user before a non read-only tool (annotations),
        // and each call checks the scope of the token again.
        foreach (MethodInfo method in ToolMethods(toolAssemblies))
        {
            string scope = RequiredScope(method);
            services.AddSingleton<McpServerTool>(serviceProvider => new ScopedMcpServerTool(
                McpServerTool.Create(method, options: new McpServerToolCreateOptions { Services = serviceProvider }),
                scope));
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

    /// <summary>The scope a tool needs: <c>pricely.read</c> to read, its <c>[ToolScope]</c> or <c>pricely.write</c> to write.</summary>
    public static string RequiredScope(MethodInfo method) =>
        method.GetCustomAttribute<McpServerToolAttribute>()!.ReadOnly
            ? PricelyScopes.Read
            : method.GetCustomAttribute<ToolScopeAttribute>()?.Scope ?? PricelyScopes.Write;

    internal static IReadOnlySet<string> ScopesOf(IServiceProvider? services) =>
        services?.GetService<ICurrentUser>()?.Scopes ?? new HashSet<string>(StringComparer.Ordinal);

    private static string ToolName(MethodInfo method) => method.GetCustomAttribute<McpServerToolAttribute>()!.Name ?? method.Name;

    /// <summary>
    /// The same tool methods as agent tools (ADR-009): every <c>[McpServerTool]</c> method, read-only or not, as an
    /// <see cref="AIFunction"/> whose service parameters (interfaces such as <c>ISender</c>) are resolved from the services
    /// of the call instead of being exposed to the model. <c>[ToolScope]</c> gives the approval scope of a write tool.
    /// </summary>
    public static IEnumerable<AgentTool> ToAgentTools(IEnumerable<Assembly> toolAssemblies) =>
        ToolMethods(toolAssemblies).Select(ToAgentTool);

    /// <summary>
    /// Static methods marked <c>[McpServerTool]</c> of the <c>[McpServerToolType]</c> classes of the assemblies.
    /// </summary>
    public static IEnumerable<MethodInfo> ToolMethods(IEnumerable<Assembly> toolAssemblies) =>
        toolAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null);

    private static AgentTool ToAgentTool(MethodInfo method)
    {
        McpServerToolAttribute attribute = method.GetCustomAttribute<McpServerToolAttribute>()!;
        string name = attribute.Name ?? method.Name;

        AIFunction function = AIFunctionFactory.Create(
            method,
            target: null,
            new AIFunctionFactoryOptions
            {
                Name = name,
                ConfigureParameterBinding = parameter => parameter.ParameterType.IsInterface
                    ? new AIFunctionFactoryOptions.ParameterBindingOptions
                    {
                        ExcludeFromSchema = true,
                        BindParameter = (p, arguments) =>
                            (arguments.Services ?? throw new InvalidOperationException("The tool call has no services."))
                            .GetRequiredService(p.ParameterType)
                    }
                    : default
            });

        return new AgentTool(name, attribute.ReadOnly, method.GetCustomAttribute<ToolScopeAttribute>()?.Scope, function);
    }

    /// <summary>
    /// Maps the MCP endpoint. Requires the <c>pricely.read</c> scope (ADR-010).
    /// </summary>
    public static IEndpointConventionBuilder MapPricelyMcp(this IEndpointRouteBuilder app) =>
        app.MapMcp(McpRoute).RequireAuthorization(PricelyScopes.Read);
}
