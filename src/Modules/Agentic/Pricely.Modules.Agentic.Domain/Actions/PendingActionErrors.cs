using Pricely.Common.Domain;

namespace Pricely.Modules.Agentic.Domain.Actions;

public static class PendingActionErrors
{
    public const string MissingScopeCode = "Actions.MissingScope";

    public static Error NotFound(Guid actionId) =>
        Error.NotFound("Actions.NotFound", $"The action {actionId} was not found.");

    public static Error AlreadyDecided(Guid actionId) =>
        Error.Conflict("Actions.AlreadyDecided", $"The action {actionId} has already been approved or rejected.");

    public static Error BatchNotFound(Guid batchId) =>
        Error.NotFound("Actions.BatchNotFound", $"The batch {batchId} was not found.");

    public static Error BatchAlreadyDecided(Guid batchId) =>
        Error.Conflict("Actions.BatchAlreadyDecided", $"The actions of the batch {batchId} have already been approved or rejected.");

    public static Error MissingScope(string scope) =>
        Error.Failure(MissingScopeCode, $"Approving this action requires the '{scope}' scope.");

    public static Error UnknownTool(string toolName) =>
        Error.NotFound("Actions.UnknownTool", $"The tool '{toolName}' is not available.");
}
