using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Agentic.Application.Actions.RejectBatch;

/// <summary>Rejects every action of a batch still pending: nothing is changed (ADR-028).</summary>
public sealed record RejectBatchCommand(Guid BatchId) : ICommand<IReadOnlyList<PendingActionResponse>>;
