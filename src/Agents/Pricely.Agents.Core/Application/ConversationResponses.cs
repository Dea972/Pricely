using System.Text.Json;
using Pricely.Agents.Core.Domain;

namespace Pricely.Agents.Core.Application;

public sealed record ConversationSummaryResponse(Guid Id, string AgentName, string Title, DateTime StartedOnUtc, DateTime LastActivityOnUtc);

public sealed record ConversationResponse(
    Guid Id,
    string AgentName,
    string Title,
    DateTime StartedOnUtc,
    IReadOnlyCollection<ConversationMessageResponse> Messages,
    IReadOnlyCollection<PendingActionResponse> Actions);

/// <summary>
/// A message as shown to the user: text, the tools called (with their typed JSON results), in order.
/// Notes for the model (approved / rejected actions) are not included: the actions carry their status.
/// </summary>
public sealed record ConversationMessageResponse(
    int Sequence,
    string Role,
    string Text,
    IReadOnlyCollection<ToolCallEvent> ToolCalls,
    IReadOnlyCollection<ToolResultEvent> ToolResults,
    DateTime CreatedOnUtc);

public sealed record PendingActionResponse(
    Guid Id,
    Guid ConversationId,
    string ToolName,
    JsonElement Arguments,
    string RequiredScope,
    string Status,
    DateTime ProposedOnUtc,
    DateTime? DecidedOnUtc,
    JsonElement? Result,
    string? Error)
{
    public static PendingActionResponse From(PendingAction action) =>
        new(
            action.Id,
            action.ConversationId,
            action.ToolName,
            JsonDocument.Parse(action.Arguments).RootElement.Clone(),
            action.RequiredScope,
            action.Status.ToString(),
            action.ProposedOnUtc,
            action.DecidedOnUtc,
            action.Result is null ? null : JsonDocument.Parse(action.Result).RootElement.Clone(),
            action.Error);
}
