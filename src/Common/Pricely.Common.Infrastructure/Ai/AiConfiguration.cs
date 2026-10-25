using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Infrastructure.Ai.Anthropic;

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

        // Replaced by the AiEngine module when it is loaded.
        services.TryAddScoped<ILlmTargetResolver, DefaultLlmTargetResolver>();

        return services;
    }
}
