using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Agents.Core.Application;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Infrastructure.Authentication;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;

namespace Pricely.Agents.Core.Endpoints;

internal sealed class GetConversations : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "agents/conversations",
            async (string? agentName, ClaimsPrincipal user, ConversationQueries queries, CancellationToken cancellationToken) =>
                user.FindUserId() is Guid userId
                    ? Results.Ok(await queries.ListAsync(userId, agentName, cancellationToken))
                    : Results.Forbid())
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Agents);
    }
}

internal sealed class GetConversation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "agents/conversations/{conversationId:guid}",
            async (Guid conversationId, ClaimsPrincipal user, ConversationQueries queries, CancellationToken cancellationToken) =>
            {
                if (user.FindUserId() is not Guid userId)
                {
                    return Results.Forbid();
                }

                Result<ConversationResponse> result = await queries.GetAsync(conversationId, userId, cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Agents);
    }
}
