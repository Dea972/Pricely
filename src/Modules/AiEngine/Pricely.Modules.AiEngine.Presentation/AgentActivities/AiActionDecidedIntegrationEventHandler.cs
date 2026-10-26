using MediatR;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.AgentActivities.RecordActionDecision;
using Pricely.Modules.AiEngine.Domain.AgentActivities;

namespace Pricely.Modules.AiEngine.Presentation.AgentActivities;

internal sealed class AiActionDecidedIntegrationEventHandler(ISender sender)
    : IntegrationEventHandler<AiActionDecidedIntegrationEvent>
{
    public override async Task Handle(AiActionDecidedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        AgentActionDecision decision = Enum.TryParse(integrationEvent.Decision, out AgentActionDecision parsed)
            ? parsed
            : throw new PricelyException($"Unknown decision '{integrationEvent.Decision}'.");

        Result result = await sender.Send(
            new RecordActionDecisionCommand(
                new AgentActivityDetails(
                    integrationEvent.Id,
                    integrationEvent.DecidedOnUtc ?? integrationEvent.OccurredOnUtc,
                    integrationEvent.SessionId,
                    integrationEvent.FeatureCode,
                    integrationEvent.AgentName,
                    integrationEvent.ToolName,
                    integrationEvent.UserId),
                integrationEvent.ActionId,
                decision,
                integrationEvent.ProposedOnUtc),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new PricelyException(nameof(RecordActionDecisionCommand), result.Error);
        }
    }
}
