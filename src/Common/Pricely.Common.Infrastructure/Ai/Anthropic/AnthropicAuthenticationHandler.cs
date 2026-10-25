using Microsoft.Extensions.Options;

namespace Pricely.Common.Infrastructure.Ai.Anthropic;

internal sealed class AnthropicAuthenticationHandler(IOptions<AnthropicOptions> options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove("x-api-key");
        request.Headers.Add("x-api-key", options.Value.ApiKey);
        request.Headers.Remove("anthropic-version");
        request.Headers.Add("anthropic-version", options.Value.ApiVersion);

        return base.SendAsync(request, cancellationToken);
    }
}
