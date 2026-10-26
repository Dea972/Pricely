using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Agentic.Application.Conversations.GetConversation;
using Pricely.Modules.Agentic.Application.Conversations.GetConversations;

namespace Pricely.Modules.Agentic.Presentation.Agents;

internal sealed class GetConversations : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "agents/conversations",
            async (string? agentName, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not Guid userId)
                {
                    return Results.Forbid();
                }

                Result<IReadOnlyCollection<ConversationSummaryResponse>> result = await sender.Send(
                    new GetConversationsQuery(userId, agentName),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAgenticAccess(app)
        .WithTags(Tags.Agents);
    }
}

internal sealed class GetConversation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "agents/conversations/{conversationId:guid}",
            async (Guid conversationId, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not Guid userId)
                {
                    return Results.Forbid();
                }

                Result<ConversationResponse> result = await sender.Send(new GetConversationQuery(conversationId, userId), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAgenticAccess(app)
        .WithTags(Tags.Agents);
    }
}
