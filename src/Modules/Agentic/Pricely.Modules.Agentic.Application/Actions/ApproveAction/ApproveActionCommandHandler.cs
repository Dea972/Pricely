using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Domain.Actions;

namespace Pricely.Modules.Agentic.Application.Actions.ApproveAction;

/// <summary>
/// Saves the approval first (under optimistic concurrency: a second approval fails), then runs the tool directly with the
/// stored arguments, without going back to the model, and saves the outcome (<see cref="ApprovedActionRunner"/>). Only
/// the action is written: the next turn replays the decision to the model (see <c>ConversationNotes</c>), the outbox
/// publishes it to the AiEngine journal.
/// </summary>
internal sealed class ApproveActionCommandHandler(
    IPendingActionRepository pendingActions,
    IUnitOfWork unitOfWork,
    ApprovedActionRunner runner,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ApproveActionCommand, PendingActionResponse>
{
    public async Task<Result<PendingActionResponse>> Handle(ApproveActionCommand request, CancellationToken cancellationToken)
    {
        PendingAction? action = await pendingActions.GetAsync(request.ActionId, cancellationToken);
        if (action is null || currentUser.UserId is not Guid userId || action.UserId != userId)
        {
            // Not the owner: the action does not exist for this caller.
            return Result.Failure<PendingActionResponse>(PendingActionErrors.NotFound(request.ActionId));
        }

        Result<(AgentTool Tool, AgentDefinition Agent)> resolved = runner.Resolve(action);
        if (resolved.IsFailure)
        {
            return Result.Failure<PendingActionResponse>(resolved.Error);
        }

        Result approved = action.Approve(userId, currentUser.Scopes, dateTimeProvider.UtcNow);
        if (approved.IsFailure)
        {
            return Result.Failure<PendingActionResponse>(approved.Error);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUpdateException)
        {
            // Another request approved or rejected it in the meantime: the tool must not run twice.
            return Result.Failure<PendingActionResponse>(PendingActionErrors.AlreadyDecided(request.ActionId));
        }

        await runner.RunAsync(action, resolved.Value.Tool, resolved.Value.Agent);

        return PendingActionResponse.From(action);
    }
}
