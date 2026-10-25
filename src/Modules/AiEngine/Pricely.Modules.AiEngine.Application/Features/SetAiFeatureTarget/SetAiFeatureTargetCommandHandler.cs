using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.Features;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Application.Features.SetAiFeatureTarget;

internal sealed class SetAiFeatureTargetCommandHandler(
    IFeatureSettingRepository featureSettingRepository,
    IModelRepository modelRepository,
    IEnumerable<AiFeature> features,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetAiFeatureTargetCommand>
{
    public async Task<Result> Handle(SetAiFeatureTargetCommand request, CancellationToken cancellationToken)
    {
        bool isKnownFeature = FeatureSetting.IsDefaultCode(request.FeatureCode) ||
            features.Any(f => string.Equals(f.Code, request.FeatureCode, StringComparison.OrdinalIgnoreCase));
        if (!isKnownFeature)
        {
            return Result.Failure(FeatureSettingErrors.UnknownFeature(request.FeatureCode));
        }

        if (await modelRepository.GetAsync(request.ModelId, cancellationToken) is null)
        {
            return Result.Failure(ModelErrors.NotFound(request.ModelId));
        }

        FeatureSetting? setting = await featureSettingRepository.GetAsync(request.FeatureCode, cancellationToken);
        if (setting is null)
        {
            Result<FeatureSetting> created = FeatureSetting.Create(request.FeatureCode, request.ModelId, dateTimeProvider.UtcNow);
            if (created.IsFailure)
            {
                return created;
            }

            featureSettingRepository.Insert(created.Value);
        }
        else
        {
            setting.Target(request.ModelId, dateTimeProvider.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
