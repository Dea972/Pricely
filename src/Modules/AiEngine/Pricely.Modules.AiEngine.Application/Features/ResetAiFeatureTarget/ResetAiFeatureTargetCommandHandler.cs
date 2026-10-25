using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.Features;

namespace Pricely.Modules.AiEngine.Application.Features.ResetAiFeatureTarget;

internal sealed class ResetAiFeatureTargetCommandHandler(
    IFeatureSettingRepository featureSettingRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ResetAiFeatureTargetCommand>
{
    public async Task<Result> Handle(ResetAiFeatureTargetCommand request, CancellationToken cancellationToken)
    {
        FeatureSetting? setting = await featureSettingRepository.GetAsync(request.FeatureCode, cancellationToken);
        if (setting is null)
        {
            return Result.Failure(FeatureSettingErrors.NotFound(request.FeatureCode));
        }

        featureSettingRepository.Remove(setting);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
