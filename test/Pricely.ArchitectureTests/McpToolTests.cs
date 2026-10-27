using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Presentation.Mcp;
using Pricely.Modules.Catalog.Infrastructure;
using Pricely.Modules.Pricing.Infrastructure;

namespace Pricely.ArchitectureTests;

/// <summary>
/// The tools declared by the modules are discovered, annotated, and their services (ISender, CancellationToken) are
/// injected instead of being exposed as tool arguments. Each tool needs the scope of ADR-016.
/// </summary>
public sealed class McpToolTests
{
    private static readonly Dictionary<string, string> ExpectedScopes = new(StringComparer.Ordinal)
    {
        ["get_category_tree"] = PricelyScopes.Read,
        ["search_products"] = PricelyScopes.Read,
        ["get_product"] = PricelyScopes.Read,
        ["list_merchants"] = PricelyScopes.Read,
        ["compare_prices"] = PricelyScopes.Read,
        ["get_price_history"] = PricelyScopes.Read,
        ["list_price_anomalies"] = PricelyScopes.Read,
        ["get_collection_plan"] = PricelyScopes.Read,
        ["list_offers_pending_review"] = PricelyScopes.Read,
        ["fetch_merchant_page"] = PricelyScopes.Read,
        ["search_web"] = PricelyScopes.Read,
        ["read_web_page"] = PricelyScopes.Read,
        ["collect_product_offers"] = PricelyScopes.Write,
        ["find_or_add_product"] = PricelyScopes.Write,
        ["find_or_add_merchant"] = PricelyScopes.Write,
        ["add_web_offer"] = PricelyScopes.Write,
        ["submit_collected_offer"] = PricelyScopes.Write,
        ["confirm_offer"] = PricelyScopes.Review,
        ["reject_offer"] = PricelyScopes.Review
    };

    private static readonly Assembly[] ToolAssemblies = [CatalogModule.ToolAssembly, PricingModule.ToolAssembly];

    [Fact]
    public void Tools_ShouldBeDiscovered_Annotated_AndWithoutServiceArguments()
    {
        using ServiceProvider serviceProvider = BuildServices();

        var tools = serviceProvider.GetServices<McpServerTool>().ToList();

        tools.Select(t => t.ProtocolTool.Name).Should().BeEquivalentTo(ExpectedScopes.Keys);

        foreach (McpServerTool tool in tools)
        {
            tool.ProtocolTool.Description.Should().NotBeNullOrWhiteSpace();
            tool.ProtocolTool.Annotations?.ReadOnlyHint.Should().Be(
                ExpectedScopes[tool.ProtocolTool.Name] == PricelyScopes.Read,
                "the MCP client asks its user before a tool that is not read-only ({0})",
                tool.ProtocolTool.Name);

            string schema = JsonSerializer.Serialize(tool.ProtocolTool.InputSchema);
            schema.Should().NotContain("\"sender\"").And.NotContain("cancellationToken");
        }
    }

    [Fact]
    public void EachTool_ShouldRequireTheScopeOfItsEffect()
    {
        McpServerExtensions.ToolMethods(ToolAssemblies)
            .ToDictionary(m => m.GetCustomAttribute<McpServerToolAttribute>()!.Name!, McpServerExtensions.RequiredScope, StringComparer.Ordinal)
            .Should().BeEquivalentTo(ExpectedScopes);
    }

    [Fact]
    public void Prompts_ShouldBeDiscovered()
    {
        using ServiceProvider serviceProvider = BuildServices();

        serviceProvider.GetServices<McpServerPrompt>().Select(p => p.ProtocolPrompt.Name).Should().Contain("collect_product_prices");
    }

    private static ServiceProvider BuildServices()
    {
        ServiceCollection services = new();
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(ToolAssemblies));

        services.AddPricelyMcpServer(ToolAssemblies);

        return services.BuildServiceProvider();
    }
}
