using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Pricely.Infrastructure;

namespace Pricely.ArchitectureTests;

/// <summary>
/// The composition root can be built with the validations Program.cs enables: every dependency of every registered
/// service resolves (module handlers, keyed LLM providers, MassTransit consumers, Quartz jobs) and no scoped service
/// is captured by a singleton. Nothing connects to a database or to the identity provider.
/// </summary>
public sealed class StartupCompositionTests
{
    [Fact]
    public async Task AddPricelyAsync_ShouldProduceAValidServiceProvider()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Test" });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
                ["Authentication:Authority"] = "http://localhost:8180/realms/pricely",
                ["Authentication:RequireHttpsMetadata"] = "false",
                ["Ai:Anthropic:ApiKey"] = "test-key",
                ["Catalog:Outbox:IntervalInSeconds"] = "5",
                ["Catalog:Outbox:BatchSize"] = "20",
                ["Users:Outbox:IntervalInSeconds"] = "5",
                ["Users:Outbox:BatchSize"] = "20",
                ["Pricing:Outbox:IntervalInSeconds"] = "5",
                ["Pricing:Outbox:BatchSize"] = "20",
                ["Pricing:Inbox:IntervalInSeconds"] = "5",
                ["Pricing:Inbox:BatchSize"] = "20",
                ["Pricing:Collection:Enabled"] = "false",
                ["AiEngine:Inbox:IntervalInSeconds"] = "5",
                ["AiEngine:Inbox:BatchSize"] = "50"
            });

        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        await builder.Services.AddPricelyAsync(
            "Host=localhost;Database=pricely-db",
            builder.Configuration);

        Action build = () =>
        {
            WebApplication app = builder.Build();
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        };

        build.Should().NotThrow();
    }
}
