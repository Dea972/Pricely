using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Application.Models.RegisterModel;

internal sealed class RegisterModelCommandHandler(
    IModelRepository modelRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterModelCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterModelCommand request, CancellationToken cancellationToken)
    {
        Provider? provider = Providers.FromReportedName(request.Provider);
        if (provider is null)
        {
            return Result.Failure<Guid>(ModelErrors.UnknownProvider(request.Provider));
        }

        if (await modelRepository.ExistsAsync(provider.Value, request.ProviderModelId.Trim(), cancellationToken))
        {
            return Result.Failure<Guid>(ModelErrors.AlreadyExists);
        }

        Result<Model> model = Model.Create(provider.Value, request.ProviderModelId, request.Label, dateTimeProvider.UtcNow);
        if (model.IsFailure)
        {
            return Result.Failure<Guid>(model.Error);
        }

        modelRepository.Insert(model.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return model.Value.Id;
    }
}
