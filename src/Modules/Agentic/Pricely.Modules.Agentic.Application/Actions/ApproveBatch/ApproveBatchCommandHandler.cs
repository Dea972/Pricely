using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Domain.Actions;

namespace Pricely.Modules.Agentic.Application.Actions.ApproveBatch;

/// <summary>
/// One decision for the whole batch: every pending action is approved, or none (an unknown tool, a missing scope or a
/// concurrent decision refuses the batch), in one save. The approved actions then run one after the other, in the
/// order proposed, each outcome saved as it comes: a failed action does not stop the next ones (a later action that
/// needed it fails in turn, with its own error). Actions already decided one by one are left as they are.
/// </summary>
internal sealed class ApproveBatchCommandHandler(
    IPendingActionRepository pendingActions,
    IUnitOfWork unitOfWork,
    ApprovedActionRunner runner,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ApproveBatchCommand, IReadOnlyList<PendingActionResponse>>
{
    public async Task<Result<IReadOnlyList<PendingActionResponse>>> Handle(ApproveBatchCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<PendingAction> batch = await pendingActions.GetBatchAsync(request.BatchId, cancellationToken);
        if (batch.Count == 0 || currentUser.UserId is not Guid userId || batch.Any(a => a.UserId != userId))
        {
            return Result.Failure<IReadOnlyList<PendingActionResponse>>(PendingActionErrors.BatchNotFound(request.BatchId));
        }

        List<PendingAction> pending = [.. batch.Where(a => a.Status == PendingActionStatus.Pending)];
        if (pending.Count == 0)
        {
            return Result.Failure<IReadOnlyList<PendingActionResponse>>(PendingActionErrors.BatchAlreadyDecided(request.BatchId));
        }

        var toRun = new List<(PendingAction Action, AgentTool Tool, AgentDefinition Agent)>();
        DateTime utcNow = dateTimeProvider.UtcNow;
        foreach (PendingAction action in pending)
        {
            Result<(AgentTool Tool, AgentDefinition Agent)> resolved = runner.Resolve(action);
            if (resolved.IsFailure)
            {
                return Result.Failure<IReadOnlyList<PendingActionResponse>>(resolved.Error);
            }

            Result approved = action.Approve(userId, currentUser.Scopes, utcNow);
            if (approved.IsFailure)
            {
                return Result.Failure<IReadOnlyList<PendingActionResponse>>(approved.Error);
            }

            toRun.Add((action, resolved.Value.Tool, resolved.Value.Agent));
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUpdateException)
        {
            // An action of the batch was decided in the meantime: nothing was approved, nothing runs.
            return Result.Failure<IReadOnlyList<PendingActionResponse>>(PendingActionErrors.BatchAlreadyDecided(request.BatchId));
        }

        foreach ((PendingAction action, AgentTool tool, AgentDefinition agent) in toRun)
        {
            await runner.RunAsync(action, tool, agent);
        }

        return Result.Success<IReadOnlyList<PendingActionResponse>>([.. batch.Select(PendingActionResponse.From)]);
    }
}
