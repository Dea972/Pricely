using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.LlmCalls;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Application.LlmCalls.RecordLlmCall;

internal sealed class RecordLlmCallCommandHandler(
    IModelRepository modelRepository,
    ILlmCallRepository llmCallRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RecordLlmCallCommand>
{
    public async Task<Result> Handle(RecordLlmCallCommand request, CancellationToken cancellationToken)
    {
        Provider? provider = Providers.FromReportedName(request.Details.ModelProvider);
        Model? model = provider is null
            ? null
            : await modelRepository.FindAsync(provider.Value, request.Details.ModelName, cancellationToken);

        llmCallRepository.Insert(LlmCall.Record(request.Details, model));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
