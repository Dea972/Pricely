using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Agentic.Application.Actions.RejectAction;

/// <summary>
/// Rejects a pending action of the caller: nothing is changed.
/// </summary>
public sealed record RejectActionCommand(Guid ActionId) : ICommand<PendingActionResponse>;
