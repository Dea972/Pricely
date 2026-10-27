using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Domain.Actions;

namespace Pricely.Modules.Agentic.Application.Actions.RejectBatch;

internal sealed class RejectBatchCommandHandler(
    IPendingActionRepository pendingActions,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RejectBatchCommand, IReadOnlyList<PendingActionResponse>>
{
    public async Task<Result<IReadOnlyList<PendingActionResponse>>> Handle(RejectBatchCommand request, CancellationToken cancellationToken)
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

        DateTime utcNow = dateTimeProvider.UtcNow;
        foreach (PendingAction action in pending)
        {
            Result rejected = action.Reject(userId, utcNow);
            if (rejected.IsFailure)
            {
                return Result.Failure<IReadOnlyList<PendingActionResponse>>(rejected.Error);
            }
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUpdateException)
        {
            return Result.Failure<IReadOnlyList<PendingActionResponse>>(PendingActionErrors.BatchAlreadyDecided(request.BatchId));
        }

        return Result.Success<IReadOnlyList<PendingActionResponse>>([.. batch.Select(PendingActionResponse.From)]);
    }
}
