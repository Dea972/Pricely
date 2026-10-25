using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Pricely.Modules.Catalog.Infrastructure;
using Pricely.Modules.Pricing.Infrastructure;

namespace Pricely.ArchitectureTests;

/// <summary>
/// The tools declared by the modules are discovered, read-only, and their services (ISender, CancellationToken)
/// are injected instead of being exposed as tool arguments.
/// </summary>
public sealed class McpToolTests
{
    private static readonly string[] ExpectedTools =
    [
        "get_category_tree",
        "search_products",
        "get_product",
        "list_merchants",
        "compare_prices",
        "get_price_history",
        "list_price_anomalies",
        "get_collection_plan",
        "list_offers_pending_review"
    ];

    [Fact]
    public void Tools_ShouldBeDiscovered_ReadOnly_AndWithoutServiceArguments()
    {
        using ServiceProvider serviceProvider = BuildServices(CatalogModule.ToolAssembly, PricingModule.ToolAssembly);

        List<McpServerTool> tools = serviceProvider.GetServices<McpServerTool>().ToList();

        tools.Select(t => t.ProtocolTool.Name).Should().BeEquivalentTo(ExpectedTools);

        foreach (McpServerTool tool in tools)
        {
            tool.ProtocolTool.Description.Should().NotBeNullOrWhiteSpace();
            tool.ProtocolTool.Annotations?.ReadOnlyHint.Should().BeTrue();

            string schema = JsonSerializer.Serialize(tool.ProtocolTool.InputSchema);
            schema.Should().NotContain("\"sender\"").And.NotContain("cancellationToken");
        }
    }

    private static ServiceProvider BuildServices(params Assembly[] toolAssemblies)
    {
        ServiceCollection services = new();
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(toolAssemblies));

        IMcpServerBuilder builder = services.AddMcpServer();
        foreach (Assembly toolAssembly in toolAssemblies)
        {
            builder.WithToolsFromAssembly(toolAssembly);
        }

        return services.BuildServiceProvider();
    }
}
