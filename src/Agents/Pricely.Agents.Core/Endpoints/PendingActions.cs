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

/// <summary>
/// Approves a proposed action: the tool runs now with the proposed arguments. Needs the scope of the tool (ADR-010).
/// </summary>
internal sealed class ApproveAction : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "agents/actions/{actionId:guid}/approve",
            async (Guid actionId, ClaimsPrincipal user, AgentActionService actions, CancellationToken cancellationToken) =>
            {
                Result<PendingActionResponse> result = await actions.ApproveAsync(
                    actionId,
                    user.GetUserId(),
                    user.GetScopes(),
                    cancellationToken);

                return result.IsFailure && result.Error.Code == "Actions.MissingScope"
                    ? Results.Forbid()
                    : result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Agents);
    }
}

/// <summary>
/// Rejects a proposed action: nothing is changed.
/// </summary>
internal sealed class RejectAction : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "agents/actions/{actionId:guid}/reject",
            async (Guid actionId, ClaimsPrincipal user, AgentActionService actions, CancellationToken cancellationToken) =>
            {
                Result<PendingActionResponse> result = await actions.RejectAsync(actionId, user.GetUserId(), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Agents);
    }
}
