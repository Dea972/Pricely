using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Web;
using Pricely.Common.Infrastructure.Ai.Anthropic;
using Pricely.Common.Infrastructure.Web.Search;

namespace Pricely.Common.Infrastructure.Ai;

public static class AiConfiguration
{
    /// <summary>
    /// Registers the LLM providers and the gateway, once, at startup (see <c>Pricely.Sdk</c>). Modules never register
    /// a provider themselves: they declare <see cref="AiFeature"/>s and go through <see cref="ILlmGateway"/>.
    /// </summary>
    public static IServiceCollection AddAiProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AnthropicOptions>().Bind(configuration.GetSection(AnthropicOptions.SectionName));
        services.AddTransient<AnthropicAuthenticationHandler>();

        services.AddHttpClient(AiProviderKeys.Anthropic, (serviceProvider, client) =>
            {
                AnthropicOptions options = serviceProvider.GetRequiredService<IOptions<AnthropicOptions>>().Value;
                client.BaseAddress = options.BaseAddress;
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
            })
            .AddHttpMessageHandler<AnthropicAuthenticationHandler>();

        services.AddKeyedScoped<LlmServiceBase>(AiProviderKeys.Anthropic, (serviceProvider, _) =>
            new AnthropicLlmService(
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(AiProviderKeys.Anthropic),
                serviceProvider.GetRequiredService<IOptions<AnthropicOptions>>(),
                serviceProvider.GetRequiredService<IEventBus>(),
                serviceProvider.GetRequiredService<IDateTimeProvider>()));

        services.AddScoped<ILlmGateway, LlmGateway>();

        services.AddWebSearch(configuration);

        // Replaced by the AiEngine module when it is loaded.
        services.TryAddScoped<ILlmTargetResolver, DefaultLlmTargetResolver>();

        return services;
    }

    /// <summary>
    /// The web search (ADR-027): the provider of <c>WebSearch:Provider</c> behind <see cref="IWebSearcher"/>, with its daily
    /// cap and journal. Anthropic searches through the model of <see cref="WebSearchFeatures.Search"/>.
    /// </summary>
    private static void AddWebSearch(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WebSearchOptions>().Bind(configuration.GetSection(WebSearchOptions.SectionName));
        services.AddSingleton<WebSearchQuota>();
        services.AddAiFeature(WebSearchFeatures.Search);

        services.AddHttpClient(WebSearchProviders.Brave, (serviceProvider, client) =>
        {
            client.BaseAddress = serviceProvider.GetRequiredService<IOptions<WebSearchOptions>>().Value.Brave.BaseAddress;
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddKeyedScoped<IWebSearchProvider>(WebSearchProviders.Brave, (serviceProvider, _) =>
            new BraveWebSearchProvider(
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(WebSearchProviders.Brave),
                serviceProvider.GetRequiredService<IOptionsMonitor<WebSearchOptions>>()));

        services.AddKeyedScoped<IWebSearchProvider>(WebSearchProviders.Anthropic, (serviceProvider, _) =>
            new AnthropicWebSearchProvider(
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(AiProviderKeys.Anthropic),
                serviceProvider.GetRequiredService<IOptions<AnthropicOptions>>(),
                serviceProvider.GetRequiredService<IOptionsMonitor<WebSearchOptions>>(),
                serviceProvider.GetRequiredService<ILlmTargetResolver>(),
                serviceProvider.GetRequiredService<IEventBus>(),
                serviceProvider.GetRequiredService<IDateTimeProvider>()));

        services.AddScoped<IWebSearcher, WebSearcher>();
    }
}
