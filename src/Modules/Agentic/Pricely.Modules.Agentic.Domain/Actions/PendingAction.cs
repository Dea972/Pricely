using Pricely.Common.Domain;

namespace Pricely.Modules.Agentic.Domain.Actions;

/// <summary>
/// A write tool call proposed by an agent (ADR-008, ADR-014). Nothing runs before the owner of the conversation approves it;
/// the approval runs the tool directly with the stored arguments, without going back to the model.
/// </summary>
public sealed class PendingAction : Entity
{
    public const int ToolNameMaxLength = 100;

    public const int ScopeMaxLength = 50;

    public const int ErrorMaxLength = 2000;

    private PendingAction()
    {
    }

    public Guid Id { get; private set; }

    public Guid ConversationId { get; private set; }

    /// <summary>User who owns the conversation and decides (plain identifier of the users of the application).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Agent that proposed the action.</summary>
    public string AgentName { get; private set; } = string.Empty;

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
        string agentName,
        string toolName,
        string arguments,
        string requiredScope,
        DateTime utcNow) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ConversationId = conversationId,
            UserId = userId,
            AgentName = agentName,
            ToolName = toolName,
            Arguments = arguments,
            RequiredScope = requiredScope,
            Status = PendingActionStatus.Pending,
            ProposedOnUtc = utcNow
        };

    /// <summary>
    /// Approves the action. The caller saves this state before running the tool: a second approval then fails.
    /// </summary>
    public Result Approve(Guid userId, IReadOnlySet<string> scopes, DateTime utcNow)
    {
        Result allowed = EnsureCanBeDecidedBy(userId, scopes);
        if (allowed.IsFailure)
        {
            return allowed;
        }

        Status = PendingActionStatus.Approved;
        DecidedOnUtc = utcNow;

        return Common.Domain.Result.Success();
    }

    private Result EnsureCanBeDecidedBy(Guid userId, IReadOnlySet<string> scopes)
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

    public void MarkExecuted(string result)
    {
        EnsureApproved();
        Status = PendingActionStatus.Executed;
        Result = result;
        Raise(new PendingActionDecidedDomainEvent(Id));
    }

    public void MarkFailed(string error)
    {
        EnsureApproved();
        Status = PendingActionStatus.Failed;
        Error = error.Length <= ErrorMaxLength ? error : error[..ErrorMaxLength];
        Raise(new PendingActionDecidedDomainEvent(Id));
    }

    private void EnsureApproved()
    {
        if (Status != PendingActionStatus.Approved)
        {
            throw new InvalidOperationException($"The action {Id} is {Status}, not approved.");
        }
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
        Raise(new PendingActionDecidedDomainEvent(Id));

        return Common.Domain.Result.Success();
    }
}
