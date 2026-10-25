using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.LlmCalls;
using Pricely.Modules.AiEngine.Domain.Models;
using Pricely.Modules.AiEngine.Domain.Sessions;

namespace Pricely.Modules.AiEngine.Application.LlmCalls.RecordLlmCall;

internal sealed class RecordLlmCallCommandHandler(
    ISessionRepository sessionRepository,
    IModelRepository modelRepository,
    ILlmCallRepository llmCallRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RecordLlmCallCommand>
{
    public async Task<Result> Handle(RecordLlmCallCommand request, CancellationToken cancellationToken)
    {
        Session? session = await sessionRepository.GetByExternalIdAsync(request.SessionExternalId, cancellationToken);
        if (session is null)
        {
            session = Session.Start(
                request.SessionExternalId,
                request.SourceModule,
                request.UserId,
                request.Details.CalledOnUtc);
            sessionRepository.Insert(session);
        }

        Provider? provider = Providers.FromReportedName(request.Details.ModelProvider);
        Model? model = provider is null
            ? null
            : await modelRepository.FindAsync(provider.Value, request.Details.ModelName, cancellationToken);

        llmCallRepository.Insert(LlmCall.Record(session.Id, request.Details, model));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
