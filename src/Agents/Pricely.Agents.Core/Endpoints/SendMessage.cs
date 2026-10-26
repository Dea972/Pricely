using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Agents.Core.Application;
using Pricely.Agents.Core.Domain;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Infrastructure.Authentication;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;

namespace Pricely.Agents.Core.Endpoints;

/// <summary>
/// Sends a message to an agent and streams its turn as server-sent events (<see cref="AgentEvent"/>): the conversation,
/// the tools called and their typed results, the proposed actions, the text, then <c>done</c>.
/// </summary>
internal sealed class SendMessage : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "agents/{agentName}/messages",
            async (string agentName, Request request, ClaimsPrincipal user, AgentRunner runner, CancellationToken cancellationToken) =>
            {
                if (user.FindUserId() is not Guid userId)
                {
                    return Results.Forbid();
                }

                Result<AgentConversation> conversation = await runner.PrepareAsync(
                    agentName,
                    request.ConversationId,
                    request.Message,
                    userId,
                    cancellationToken);

                return conversation.IsFailure
                    ? ApiResults.Problem(conversation)
                    : TypedResults.ServerSentEvents(Stream(runner, conversation.Value, request.Message, cancellationToken));
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Agents);
    }

    private static async IAsyncEnumerable<SseItem<object>> Stream(
        AgentRunner runner,
        AgentConversation conversation,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (AgentEvent agentEvent in runner.RunAsync(conversation, message, cancellationToken))
        {
            yield return new SseItem<object>(agentEvent.Data, agentEvent.Type);
        }
    }

    internal sealed class Request
    {
        public Guid? ConversationId { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
