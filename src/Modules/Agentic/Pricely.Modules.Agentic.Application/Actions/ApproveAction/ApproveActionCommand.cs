using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Agentic.Application.Actions.ApproveAction;

/// <summary>
/// Approves a pending action of the caller: the tool runs now, once, with the proposed arguments.
/// </summary>
public sealed record ApproveActionCommand(Guid ActionId) : ICommand<PendingActionResponse>;
