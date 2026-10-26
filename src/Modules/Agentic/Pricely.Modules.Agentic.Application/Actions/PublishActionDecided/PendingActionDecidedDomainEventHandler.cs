using Pricely.Common.Application.AI;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Application.Messaging;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Domain.Actions;

namespace Pricely.Modules.Agentic.Application.Actions.PublishActionDecided;

/// <summary>
/// Publishes the decision on an action to the AiEngine journal, through the outbox of the module.
/// </summary>
internal sealed class PendingActionDecidedDomainEventHandler(
    IPendingActionRepository pendingActions,
    AgentCatalog catalog,
    IEventBus eventBus)
    : DomainEventHandler<PendingActionDecidedDomainEvent>
{
    public override async Task Handle(PendingActionDecidedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        PendingAction action = await pendingActions.GetAsync(domainEvent.ActionId, cancellationToken)
            ?? throw new PricelyException(nameof(PendingActionDecidedDomainEventHandler), PendingActionErrors.NotFound(domainEvent.ActionId));

        AgentDefinition? agent = catalog.FindAgent(action.AgentName);

        await eventBus.PublishAsync(
            new AiActionDecidedIntegrationEvent(domainEvent.Id, domainEvent.OccurredOnUtc)
            {
                ActionId = action.Id,
                SessionId = action.ConversationId.ToString(),
                FeatureCode = agent?.Feature.Code ?? $"agents.{action.AgentName}",
                SourceModule = agent?.Feature.SourceModule,
                AgentName = action.AgentName,
                ToolName = action.ToolName,
                Decision = action.Status switch
                {
                    PendingActionStatus.Executed => AiActionDecidedIntegrationEvent.Executed,
                    PendingActionStatus.Failed => AiActionDecidedIntegrationEvent.Failed,
                    _ => AiActionDecidedIntegrationEvent.Rejected
                },
                UserId = action.UserId.ToString(),
                ProposedOnUtc = action.ProposedOnUtc,
                DecidedOnUtc = action.DecidedOnUtc
            },
            cancellationToken);
    }
}
