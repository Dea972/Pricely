using MediatR;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.AgentActivities.RecordToolInvocation;
using Pricely.Modules.AiEngine.Domain.AgentActivities;

namespace Pricely.Modules.AiEngine.Presentation.AgentActivities;

internal sealed class AiToolInvokedIntegrationEventHandler(ISender sender)
    : IntegrationEventHandler<AiToolInvokedIntegrationEvent>
{
    public override async Task Handle(AiToolInvokedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new RecordToolInvocationCommand(
                new AgentActivityDetails(
                    integrationEvent.Id,
                    integrationEvent.OccurredOnUtc,
                    integrationEvent.SessionId,
                    integrationEvent.FeatureCode,
                    integrationEvent.AgentName,
                    integrationEvent.ToolName,
                    integrationEvent.UserId),
                integrationEvent.IsReadOnly,
                integrationEvent.WasApproved,
                integrationEvent.DurationMs,
                integrationEvent.IsSuccess ? null : integrationEvent.ErrorMessage ?? "Failed"),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new PricelyException(nameof(RecordToolInvocationCommand), result.Error);
        }
    }
}
