using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Agentic.Application.Conversations.RunTurn;
using Pricely.Modules.Agentic.Application.Conversations.StartTurn;

namespace Pricely.Modules.Agentic.Presentation.Agents;

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
            async (string agentName, Request request, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                {
                    return Results.Forbid();
                }

                Result<TurnStartedResponse> started = await sender.Send(
                    new StartTurnCommand(agentName, request.ConversationId, request.Message),
                    cancellationToken);

                return started.IsFailure
                    ? ApiResults.Problem(started)
                    : TypedResults.ServerSentEvents(Stream(sender, new RunTurnStreamRequest(started.Value.ConversationId, request.Message), cancellationToken));
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Agents);
    }

    private static async IAsyncEnumerable<SseItem<object>> Stream(
        ISender sender,
        RunTurnStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (AgentEvent agentEvent in sender.CreateStream(request, cancellationToken))
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
