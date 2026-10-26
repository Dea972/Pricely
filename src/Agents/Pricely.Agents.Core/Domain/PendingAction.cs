using Pricely.Common.Domain;

namespace Pricely.Agents.Core.Domain;

/// <summary>
/// A write tool call proposed by an agent (ADR-008). Nothing runs before the owner of the conversation approves it;
/// the approval runs the tool directly with the stored arguments, without going back to the model.
/// </summary>
public sealed class PendingAction : Entity
{
    private PendingAction()
    {
    }

    public Guid Id { get; private set; }

    public Guid ConversationId { get; private set; }

    /// <summary>Pricely user who owns the conversation and decides.</summary>
    public Guid UserId { get; private set; }

    public string ToolName { get; private set; } = string.Empty;

    /// <summary>Arguments of the tool call, as JSON.</summary>
    public string Arguments { get; private set; } = string.Empty;

    /// <summary>OAuth scope the caller needs to approve the action (ADR-010).</summary>
    public string RequiredScope { get; private set; } = string.Empty;

    public PendingActionStatus Status { get; private set; }

    public DateTime ProposedOnUtc { get; private set; }

    public DateTime? DecidedOnUtc { get; private set; }

    /// <summary>Result of the tool, as JSON, once executed.</summary>
    public string? Result { get; private set; }

    public string? Error { get; private set; }

    public static PendingAction Propose(
        Guid conversationId,
        Guid userId,
        string toolName,
        string arguments,
        string requiredScope,
        DateTime utcNow) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ConversationId = conversationId,
            UserId = userId,
            ToolName = toolName,
            Arguments = arguments,
            RequiredScope = requiredScope,
            Status = PendingActionStatus.Pending,
            ProposedOnUtc = utcNow
        };

    public Result EnsureCanBeDecidedBy(Guid userId, IReadOnlySet<string> scopes)
    {
        if (UserId != userId)
        {
            return Common.Domain.Result.Failure(PendingActionErrors.NotFound(Id));
        }

        if (Status != PendingActionStatus.Pending)
        {
            return Common.Domain.Result.Failure(PendingActionErrors.AlreadyDecided(Id));
        }

        return scopes.Contains(RequiredScope)
            ? Common.Domain.Result.Success()
            : Common.Domain.Result.Failure(PendingActionErrors.MissingScope(RequiredScope));
    }

    public void MarkExecuted(string result, DateTime utcNow)
    {
        Status = PendingActionStatus.Executed;
        Result = result;
        DecidedOnUtc = utcNow;
    }

    public void MarkFailed(string error, DateTime utcNow)
    {
        Status = PendingActionStatus.Failed;
        Error = error;
        DecidedOnUtc = utcNow;
    }

    public Result Reject(Guid userId, DateTime utcNow)
    {
        if (UserId != userId)
        {
            return Common.Domain.Result.Failure(PendingActionErrors.NotFound(Id));
        }

        if (Status != PendingActionStatus.Pending)
        {
            return Common.Domain.Result.Failure(PendingActionErrors.AlreadyDecided(Id));
        }

        Status = PendingActionStatus.Rejected;
        DecidedOnUtc = utcNow;

        return Common.Domain.Result.Success();
    }
}
