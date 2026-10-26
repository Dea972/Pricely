using Microsoft.Extensions.Http.Resilience;
using Pricely.Web.Authentication;

namespace Pricely.Web.Api;

internal static class ApiServiceCollectionExtensions
{
    private const string BaseAddressKey = "PricelyApi:BaseAddress";

    private static readonly TimeSpan StreamHeadersTimeout = TimeSpan.FromMinutes(2);

    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Clients of the API. The base address goes through Aspire service discovery (<c>https+http://pricely-api</c>);
    /// outside the AppHost, set <c>PricelyApi:BaseAddress</c> to the address of the API.
    /// </summary>
    public static IServiceCollection AddPricelyApi(this IServiceCollection services, IConfiguration configuration)
    {
        var baseAddress = new Uri(configuration[BaseAddressKey]
            ?? throw new InvalidOperationException($"'{BaseAddressKey}' is not configured."));

        services
            .AddHttpClient(nameof(PricelyApiClient), http =>
            {
                http.BaseAddress = baseAddress;
                http.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddTypedClient((http, sp) => new PricelyApiClient(http, sp.GetRequiredService<AccessTokenProvider>()));

        // The standard resilience handler of the service defaults retries transient failures: never a POST (approving
        // an action, starting a collection). Its 10 s / 30 s timeouts are too short for those commands, which run the
        // tool or the collection before answering.
        services.Configure<HttpStandardResilienceOptions>(
            $"{nameof(PricelyApiClient)}-standard",
            options =>
            {
                options.Retry.DisableForUnsafeHttpMethods();
                options.AttemptTimeout.Timeout = CommandTimeout;
                options.TotalRequestTimeout.Timeout = CommandTimeout;
                options.CircuitBreaker.SamplingDuration = CommandTimeout * 2;
            });

        services
            .AddHttpClient(nameof(AgentStreamClient), http =>
            {
                http.BaseAddress = baseAddress;
                http.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddTypedClient((http, sp) => new AgentStreamClient(http, sp.GetRequiredService<AccessTokenProvider>()));

        // A message is never sent twice, and the turn's first event may wait for the model: the default 10 s
        // attempt timeout only covers the response headers, the stream itself is read without a timeout.
        services.Configure<HttpStandardResilienceOptions>(
            $"{nameof(AgentStreamClient)}-standard",
            options =>
            {
                options.Retry.DisableForUnsafeHttpMethods();
                options.AttemptTimeout.Timeout = StreamHeadersTimeout;
                options.TotalRequestTimeout.Timeout = StreamHeadersTimeout;
                options.CircuitBreaker.SamplingDuration = StreamHeadersTimeout * 2;
            });

        return services;
    }
}
