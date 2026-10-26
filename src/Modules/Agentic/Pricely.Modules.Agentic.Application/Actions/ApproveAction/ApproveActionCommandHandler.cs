using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Application.Conversations;
using Pricely.Modules.Agentic.Application.Tools;
using Pricely.Modules.Agentic.Domain.Actions;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Application.Actions.ApproveAction;

/// <summary>
/// Saves the approval first (under optimistic concurrency: a second approval fails), then runs the tool directly with the
/// stored arguments, without going back to the model, and notes the outcome in the conversation for the next turn.
/// </summary>
internal sealed class ApproveActionCommandHandler(
    IPendingActionRepository pendingActions,
    IAgentConversationRepository conversations,
    IUnitOfWork unitOfWork,
    AgentCatalog catalog,
    ToolInvocationJournal journal,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    IServiceProvider services,
    ILogger<ApproveActionCommandHandler> logger)
    : ICommandHandler<ApproveActionCommand, PendingActionResponse>
{
    public async Task<Result<PendingActionResponse>> Handle(ApproveActionCommand request, CancellationToken cancellationToken)
    {
        PendingAction? action = await pendingActions.GetAsync(request.ActionId, cancellationToken);
        if (action is null || currentUser.UserId is not Guid userId)
        {
            return Result.Failure<PendingActionResponse>(PendingActionErrors.NotFound(request.ActionId));
        }

        AgentTool? tool = catalog.FindTool(action.ToolName);
        AgentDefinition? agent = catalog.FindAgent(action.AgentName);
        if (tool is null || agent is null || tool.IsReadOnly || !catalog.IsToolOf(action.AgentName, action.ToolName))
        {
            return Result.Failure<PendingActionResponse>(PendingActionErrors.UnknownTool(action.ToolName));
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

        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(action.Arguments)!
            .ToDictionary(a => a.Key, a => (object?)a.Value, StringComparer.Ordinal);
        var context = new ToolCallContext(agent.Feature, agent.Name, action.ConversationId.ToString(), userId.ToString(), WasApproved: true);

        string note;
        try
        {
            object? result = await journal.InvokeAsync(tool, new AIFunctionArguments(arguments) { Services = services }, context, CancellationToken.None);
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

        AgentConversation? conversation = await conversations.GetAsync(action.ConversationId, CancellationToken.None);
        conversation?.AppendNote(note, dateTimeProvider.UtcNow);

        // The tool has run: its outcome is saved even if the caller went away.
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return PendingActionResponse.From(action);
    }
}
