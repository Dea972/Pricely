using MediatR;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.LlmCalls.RecordLlmCall;
using Pricely.Modules.AiEngine.Domain.LlmCalls;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Presentation.LlmCalls;

internal sealed class LlmCalledIntegrationEventHandler(ISender sender)
    : IntegrationEventHandler<LlmCalledIntegrationEvent>
{
    public override async Task Handle(
        LlmCalledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new RecordLlmCallCommand(
                integrationEvent.SessionId,
                integrationEvent.SourceModule,
                integrationEvent.UserId,
                new LlmCallDetails
                {
                    CalledOnUtc = integrationEvent.OccurredOnUtc,
                    FeatureCode = integrationEvent.FeatureCode,
                    ModelProvider = integrationEvent.ModelProvider,
                    ModelName = integrationEvent.ModelName,
                    TokenUsage = new TokenUsage(
                        integrationEvent.InputTokens,
                        integrationEvent.InputCachedTokens,
                        integrationEvent.InputCacheCreationTokens,
                        integrationEvent.OutputTokens,
                        integrationEvent.OutputReasoningTokens),
                    ResponseTimeMs = integrationEvent.ResponseTimeMs,
                    IsSuccess = integrationEvent.IsSuccess,
                    ErrorMessage = integrationEvent.ErrorMessage
                }),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new PricelyException(nameof(RecordLlmCallCommand), result.Error);
        }
    }
}
