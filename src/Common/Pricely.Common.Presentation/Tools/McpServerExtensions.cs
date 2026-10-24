using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pricely.Common.Presentation.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Pricely.Common.Presentation.Endpoints;

namespace Pricely.Common.Presentation.Tools;

public static class McpServerExtensions
{

    public static IServiceCollection AddMcpServerWithTools(this IServiceCollection services, Assembly[] toolAssemblies)
    {
        services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssemblies(toolAssemblies);

        return services;
    }
   
    private static IServiceCollection WithToolsFromAssemblies(this IMcpServerBuilder builder, params Assembly[] assemblies)
    {
        IServiceCollection services = builder.Services;

        McpServerTool[] tools = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                type.IsAssignableTo(typeof(IMcpTool)))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(toolMethod => toolMethod.GetCustomAttribute<McpServerToolAttribute>() is not null)
                .Select(toolMethod =>
                    McpServerTool.Create(toolMethod, Activator.CreateInstance(type))))
            .ToArray();

        foreach (McpServerTool tool in tools)
        {
            services.AddSingleton(tool);
        }


        return services;
    }
}
