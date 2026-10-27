using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Agentic.Application.Actions.ApproveBatch;

/// <summary>Approves every action of a batch still pending, then runs them in the order proposed (ADR-028).</summary>
public sealed record ApproveBatchCommand(Guid BatchId) : ICommand<IReadOnlyList<PendingActionResponse>>;
