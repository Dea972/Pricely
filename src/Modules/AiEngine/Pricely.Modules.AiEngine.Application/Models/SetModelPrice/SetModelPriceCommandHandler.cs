using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Application.Models.SetModelPrice;

internal sealed class SetModelPriceCommandHandler(
    IModelRepository modelRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetModelPriceCommand>
{
    public async Task<Result> Handle(SetModelPriceCommand request, CancellationToken cancellationToken)
    {
        Model? model = await modelRepository.GetAsync(request.ModelId, cancellationToken);
        if (model is null)
        {
            return Result.Failure(ModelErrors.NotFound(request.ModelId));
        }

        Result result = model.SetPrice(
            request.ValidFrom,
            new PriceValues(
                request.InputPerMillion,
                request.CachedInputPerMillion,
                request.CacheWritePerMillion,
                request.OutputPerMillion),
            Enum.TryParse(request.Source, ignoreCase: true, out PriceSource source) ? source : PriceSource.Manual,
            dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
