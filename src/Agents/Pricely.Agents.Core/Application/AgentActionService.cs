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
/// Decides a pending action. Approving saves the decision first (once only, under optimistic concurrency), then runs the
/// tool directly with the stored arguments, without going back to the model; the outcome is noted in the conversation
/// so that the agent knows about it on the next turn.
/// </summary>
internal sealed class AgentActionService(
    AgentsDbContext dbContext,
    AgentToolCatalog toolCatalog,
    IEnumerable<IAgentDefinition> agents,
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

        AgentConversation? conversation = await dbContext.Conversations
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.Id == action.ConversationId, cancellationToken);

        AgentTool? tool = toolCatalog.Find(action.ToolName);
        bool allowedForAgent = conversation is not null && agents.Any(a =>
            string.Equals(a.Name, conversation.AgentName, StringComparison.Ordinal) && a.ToolNames.Contains(action.ToolName));

        if (tool is null || tool.IsReadOnly || !allowedForAgent)
        {
            return Result.Failure<PendingActionResponse>(PendingActionErrors.UnknownTool(action.ToolName));
        }

        Result approved = action.Approve(userId, scopes, dateTimeProvider.UtcNow);
        if (approved.IsFailure)
        {
            return Result.Failure<PendingActionResponse>(approved.Error);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request approved or rejected it in the meantime: the tool must not run twice.
            return Result.Failure<PendingActionResponse>(PendingActionErrors.AlreadyDecided(actionId));
        }

        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(action.Arguments)!
            .ToDictionary(a => a.Key, a => (object?)a.Value, StringComparer.Ordinal);

        string note;
        try
        {
            object? result = await tool.Function.InvokeAsync(new AIFunctionArguments(arguments) { Services = services }, CancellationToken.None);
            action.MarkExecuted(ChatMessageSerializer.SerializeValue(result));
            note = $"L'utilisateur a validé l'action {action.Id} ({action.ToolName}) ; elle a été exécutée. Résultat : {action.Result}";
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The approved action {ActionId} ({ToolName}) failed", action.Id, action.ToolName);
            action.MarkFailed(exception.Message);

            // The error text may come from outside (a merchant page, a provider): it is not passed to the model.
            note = $"L'utilisateur a validé l'action {action.Id} ({action.ToolName}), mais elle a échoué.";
        }

        // The tool has run: its outcome is saved even if the caller went away.
        Note(conversation!, note);
        await dbContext.SaveChangesAsync(CancellationToken.None);

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

        AgentConversation? conversation = await dbContext.Conversations
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.Id == action.ConversationId, cancellationToken);

        if (conversation is not null)
        {
            Note(conversation, $"L'utilisateur a refusé l'action {action.Id} ({action.ToolName}) : rien n'a été modifié.");
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<PendingActionResponse>(PendingActionErrors.AlreadyDecided(actionId));
        }

        return PendingActionResponse.From(action);
    }

    /// <summary>
    /// A note for the model, kept in its place in the conversation (a user turn, not an instruction of the system prompt)
    /// and hidden from the conversation shown to the user.
    /// </summary>
    private void Note(AgentConversation conversation, string note) =>
        conversation.Append(
            AgentMessageRole.System,
            ChatMessageSerializer.Serialize(new ChatMessage(ChatRole.User, $"[Note de Pricely, pas un message de l'utilisateur] {note}")),
            dateTimeProvider.UtcNow);
}
