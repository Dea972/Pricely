using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pricely.Agents.Core.Domain;
using Pricely.Agents.Core.Infrastructure;
using Pricely.Common.Application.Clock;
using Pricely.Common.Domain;

namespace Pricely.Agents.Core.Application;

/// <summary>
/// Decides a pending action. Approving runs the tool directly with the stored arguments, without going back to the
/// model; the decision is noted in the conversation so that the agent knows about it on the next turn.
/// </summary>
internal sealed class AgentActionService(
    AgentsDbContext dbContext,
    AgentToolCatalog toolCatalog,
    IDateTimeProvider dateTimeProvider,
    IServiceProvider services,
    ILogger<AgentActionService> logger)
{
    public async Task<Result<PendingActionResponse>> ApproveAsync(
        Guid actionId,
        Guid userId,
        IReadOnlySet<string> scopes,
        CancellationToken cancellationToken)
    {
        PendingAction? action = await dbContext.PendingActions.SingleOrDefaultAsync(a => a.Id == actionId, cancellationToken);
        if (action is null)
        {
            return Result.Failure<PendingActionResponse>(PendingActionErrors.NotFound(actionId));
        }

        Result allowed = action.EnsureCanBeDecidedBy(userId, scopes);
        if (allowed.IsFailure)
        {
            return Result.Failure<PendingActionResponse>(allowed.Error);
        }

        AgentTool? tool = toolCatalog.Find(action.ToolName);
        if (tool is null)
        {
            return Result.Failure<PendingActionResponse>(PendingActionErrors.UnknownTool(action.ToolName));
        }

        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(action.Arguments)!
            .ToDictionary(a => a.Key, a => (object?)a.Value, StringComparer.Ordinal);

        try
        {
            object? result = await tool.Function.InvokeAsync(new AIFunctionArguments(arguments) { Services = services }, cancellationToken);
            action.MarkExecuted(ChatMessageSerializer.SerializeValue(result), dateTimeProvider.UtcNow);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "The approved action {ActionId} ({ToolName}) failed", action.Id, action.ToolName);
            action.MarkFailed(exception.Message, dateTimeProvider.UtcNow);
        }

        await NoteInConversationAsync(
            action,
            action.Status == PendingActionStatus.Executed
                ? $"L'utilisateur a validé l'action {action.Id} ({action.ToolName}). Elle a été exécutée ; résultat : {action.Result}"
                : $"L'utilisateur a validé l'action {action.Id} ({action.ToolName}), mais elle a échoué : {action.Error}",
            cancellationToken);

        return PendingActionResponse.From(action);
    }

    public async Task<Result<PendingActionResponse>> RejectAsync(Guid actionId, Guid userId, CancellationToken cancellationToken)
    {
        PendingAction? action = await dbContext.PendingActions.SingleOrDefaultAsync(a => a.Id == actionId, cancellationToken);
        if (action is null)
        {
            return Result.Failure<PendingActionResponse>(PendingActionErrors.NotFound(actionId));
        }

        Result rejected = action.Reject(userId, dateTimeProvider.UtcNow);
        if (rejected.IsFailure)
        {
            return Result.Failure<PendingActionResponse>(rejected.Error);
        }

        await NoteInConversationAsync(
            action,
            $"L'utilisateur a refusé l'action {action.Id} ({action.ToolName}) : rien n'a été modifié.",
            cancellationToken);

        return PendingActionResponse.From(action);
    }

    private async Task NoteInConversationAsync(PendingAction action, string note, CancellationToken cancellationToken)
    {
        AgentConversation? conversation = await dbContext.Conversations
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.Id == action.ConversationId, cancellationToken);

        conversation?.Append(
            AgentMessageRole.System,
            ChatMessageSerializer.Serialize(new ChatMessage(ChatRole.System, note)),
            dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
