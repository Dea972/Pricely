using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Agentic.Application.Actions;
using Pricely.Modules.Agentic.Application.Actions.ApproveAction;
using Pricely.Modules.Agentic.Application.Actions.ApproveBatch;
using Pricely.Modules.Agentic.Application.Actions.RejectAction;
using Pricely.Modules.Agentic.Application.Actions.RejectBatch;
using Pricely.Modules.Agentic.Domain.Actions;

namespace Pricely.Modules.Agentic.Presentation.Agents;

/// <summary>
/// Approves a proposed action: the tool runs now, once, with the proposed arguments. Needs the scope of the tool.
/// </summary>
internal sealed class ApproveAction : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "agents/actions/{actionId:guid}/approve",
            async (Guid actionId, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                {
                    return Results.Forbid();
                }

                Result<PendingActionResponse> result = await sender.Send(new ApproveActionCommand(actionId), cancellationToken);

                return result.IsFailure && result.Error.Code == PendingActionErrors.MissingScopeCode
                    ? Results.Forbid()
                    : result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAgenticAccess(app)
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
            async (Guid actionId, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                {
                    return Results.Forbid();
                }

                Result<PendingActionResponse> result = await sender.Send(new RejectActionCommand(actionId), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAgenticAccess(app)
        .WithTags(Tags.Agents);
    }
}

/// <summary>
/// Approves the actions of a batch (the writes proposed in one turn) still pending: all or none, then run in the order
/// proposed (ADR-028). Needs the scope of each of them.
/// </summary>
internal sealed class ApproveBatch : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "agents/batches/{batchId:guid}/approve",
            async (Guid batchId, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                {
                    return Results.Forbid();
                }

                Result<IReadOnlyList<PendingActionResponse>> result = await sender.Send(new ApproveBatchCommand(batchId), cancellationToken);

                return result.IsFailure && result.Error.Code == PendingActionErrors.MissingScopeCode
                    ? Results.Forbid()
                    : result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAgenticAccess(app)
        .WithTags(Tags.Agents);
    }
}

/// <summary>
/// Rejects the actions of a batch still pending: nothing is changed.
/// </summary>
internal sealed class RejectBatch : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "agents/batches/{batchId:guid}/reject",
            async (Guid batchId, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                {
                    return Results.Forbid();
                }

                Result<IReadOnlyList<PendingActionResponse>> result = await sender.Send(new RejectBatchCommand(batchId), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAgenticAccess(app)
        .WithTags(Tags.Agents);
    }
}
