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
        Result result = await sender.Send(
            new RecordActionDecisionCommand(
                new AgentActivityDetails(
                    integrationEvent.OccurredOnUtc,
                    integrationEvent.SessionId,
                    integrationEvent.FeatureCode,
                    integrationEvent.AgentName,
                    integrationEvent.ToolName,
                    integrationEvent.UserId),
                integrationEvent.ActionId,
                integrationEvent.Decision,
                integrationEvent.ProposedOnUtc),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new PricelyException(nameof(RecordActionDecisionCommand), result.Error);
        }
    }
}
