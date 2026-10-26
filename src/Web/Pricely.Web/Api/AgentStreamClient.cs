using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Pricely.Web.Authentication;

namespace Pricely.Web.Api;

/// <summary>
/// Sends a message to an agent and reads its turn as server-sent events. A client of its own: no retry (a message is
/// not idempotent) and no timeout (a turn runs as long as the model and its tools need).
/// </summary>
internal sealed class AgentStreamClient(HttpClient http, AccessTokenProvider tokens)
{
    public async IAsyncEnumerable<AgentStreamEvent> SendMessageAsync(
        string agentName,
        Guid? conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"api/agents/{Uri.EscapeDataString(agentName)}/messages", UriKind.Relative))
        {
            Content = JsonContent.Create(new { conversationId, message }, options: PricelyApiClient.JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAccessTokenAsync(cancellationToken));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await PricelyApiClient.EnsureSuccessAsync(response, cancellationToken);

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (SseItem<string> item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
        {
            if (AgentStreamEvent.Parse(item.EventType, item.Data) is AgentStreamEvent agentEvent)
            {
                yield return agentEvent;
            }
        }
    }
}
