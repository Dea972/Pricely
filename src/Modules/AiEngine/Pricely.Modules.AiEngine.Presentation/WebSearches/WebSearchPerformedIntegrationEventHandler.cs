using MediatR;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.WebSearches.RecordWebSearch;
using Pricely.Modules.AiEngine.Domain.WebSearches;

namespace Pricely.Modules.AiEngine.Presentation.WebSearches;

internal sealed class WebSearchPerformedIntegrationEventHandler(ISender sender)
    : IntegrationEventHandler<WebSearchPerformedIntegrationEvent>
{
    public override async Task Handle(
        WebSearchPerformedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new RecordWebSearchCommand(
                new WebSearchDetails
                {
                    SessionId = integrationEvent.SessionId,
                    SearchedOnUtc = integrationEvent.OccurredOnUtc,
                    Provider = integrationEvent.Provider,
                    UserId = integrationEvent.UserId,
                    Searches = integrationEvent.Searches,
                    Results = integrationEvent.Results,
                    ResponseTimeMs = integrationEvent.ResponseTimeMs,
                    IsSuccess = integrationEvent.IsSuccess,
                    ErrorMessage = integrationEvent.ErrorMessage
                }),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new PricelyException(nameof(RecordWebSearchCommand), result.Error);
        }
    }
}
