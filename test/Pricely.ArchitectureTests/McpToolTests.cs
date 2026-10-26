using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Pricely.Common.Presentation.Mcp;
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

        var tools = serviceProvider.GetServices<McpServerTool>().ToList();

        tools.Select(t => t.ProtocolTool.Name).Should().BeEquivalentTo(ExpectedTools);

        foreach (McpServerTool tool in tools)
        {
            tool.ProtocolTool.Description.Should().NotBeNullOrWhiteSpace();
            tool.ProtocolTool.Annotations?.ReadOnlyHint.Should().BeTrue();

            string schema = JsonSerializer.Serialize(tool.ProtocolTool.InputSchema);
            schema.Should().NotContain("\"sender\"").And.NotContain("cancellationToken");
        }
    }

    [Fact]
    public void WriteTools_ShouldBeDeclared_ButNotExposedOverMcp()
    {
        Assembly[] assemblies = [CatalogModule.ToolAssembly, PricingModule.ToolAssembly];

        McpServerExtensions.ToolMethods(assemblies)
            .Select(m => m.GetCustomAttribute<McpServerToolAttribute>()!)
            .Where(a => !a.ReadOnly)
            .Select(a => a.Name)
            .Should().Contain("collect_product_offers");

        using ServiceProvider serviceProvider = BuildServices(assemblies);

        serviceProvider.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).Should().NotContain("collect_product_offers");
    }

    private static ServiceProvider BuildServices(params Assembly[] toolAssemblies)
    {
        ServiceCollection services = new();
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(toolAssemblies));

        services.AddPricelyMcpServer(toolAssemblies);

        return services.BuildServiceProvider();
    }
}
