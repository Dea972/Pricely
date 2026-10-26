using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Domain.Actions;

namespace Pricely.Modules.Agentic.Application.Actions.RejectAction;

internal sealed class RejectActionCommandHandler(
    IPendingActionRepository pendingActions,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RejectActionCommand, PendingActionResponse>
{
    public async Task<Result<PendingActionResponse>> Handle(RejectActionCommand request, CancellationToken cancellationToken)
    {
        PendingAction? action = await pendingActions.GetAsync(request.ActionId, cancellationToken);
        if (action is null || currentUser.UserId is not Guid userId)
        {
            return Result.Failure<PendingActionResponse>(PendingActionErrors.NotFound(request.ActionId));
        }

        Result rejected = action.Reject(userId, dateTimeProvider.UtcNow);
        if (rejected.IsFailure)
        {
            return Result.Failure<PendingActionResponse>(rejected.Error);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUpdateException)
        {
            return Result.Failure<PendingActionResponse>(PendingActionErrors.AlreadyDecided(request.ActionId));
        }

        return PendingActionResponse.From(action);
    }
}
