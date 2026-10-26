using Pricely.Common.Domain;

namespace Pricely.Agents.Core.Domain;

public static class PendingActionErrors
{
    public static Error NotFound(Guid actionId) =>
        Error.NotFound("Actions.NotFound", $"The action {actionId} was not found.");

    public static Error AlreadyDecided(Guid actionId) =>
        Error.Conflict("Actions.AlreadyDecided", $"The action {actionId} has already been approved or rejected.");

    public static Error MissingScope(string scope) =>
        Error.Failure("Actions.MissingScope", $"Approving this action requires the '{scope}' scope.");

    public static Error UnknownTool(string toolName) =>
        Error.NotFound("Actions.UnknownTool", $"The tool '{toolName}' is not available.");
}
