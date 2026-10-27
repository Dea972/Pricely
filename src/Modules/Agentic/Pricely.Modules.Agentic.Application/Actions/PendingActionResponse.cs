using System.Text.Json;
using Pricely.Modules.Agentic.Domain.Actions;

namespace Pricely.Modules.Agentic.Application.Actions;

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
    /// <summary>The batch of the turn that proposed the action (ADR-028).</summary>
    public Guid BatchId { get; init; }

    public static PendingActionResponse From(PendingAction action) =>
        new(
            action.Id,
            action.ConversationId,
            action.ToolName,
            Parse(action.Arguments)!.Value,
            action.RequiredScope,
            action.Status.ToString(),
            action.ProposedOnUtc,
            action.DecidedOnUtc,
            Parse(action.Result),
            action.Error)
        {
            BatchId = action.BatchId
        };

    public static JsonElement? Parse(string? json)
    {
        if (json is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }
}
