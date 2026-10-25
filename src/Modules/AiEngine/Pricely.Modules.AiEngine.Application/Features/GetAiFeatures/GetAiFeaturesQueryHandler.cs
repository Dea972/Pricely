using Pricely.Common.Application.AI;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Domain.Features;

namespace Pricely.Modules.AiEngine.Application.Features.GetAiFeatures;

internal sealed class GetAiFeaturesQueryHandler(
    IFeatureSettingRepository featureSettingRepository,
    IEnumerable<AiFeature> features)
    : IQueryHandler<GetAiFeaturesQuery, IReadOnlyCollection<AiFeatureResponse>>
{
    private const string DefaultLabel = "Default target (features without their own target)";

    public async Task<Result<IReadOnlyCollection<AiFeatureResponse>>> Handle(
        GetAiFeaturesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FeatureSetting> settings = await featureSettingRepository.GetAllAsync(cancellationToken);
        var settingsByCode = settings.ToDictionary(s => s.FeatureCode, StringComparer.OrdinalIgnoreCase);
        FeatureSetting? fallback = settingsByCode.GetValueOrDefault(FeatureSetting.DefaultCode);

        List<AiFeatureResponse> rows =
        [
            new(
                FeatureSetting.DefaultCode,
                DefaultLabel,
                null,
                null,
                fallback?.ModelId,
                fallback?.ModelId,
                fallback is null ? AiFeatureTargetSource.Provider : AiFeatureTargetSource.Default)
        ];

        IEnumerable<AiFeature> declaredFeatures = features
            .DistinctBy(f => f.Code, StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f.SourceModule, StringComparer.Ordinal)
            .ThenBy(f => f.Label, StringComparer.Ordinal);

        foreach (AiFeature feature in declaredFeatures)
        {
            FeatureSetting? own = settingsByCode.GetValueOrDefault(feature.Code);
            FeatureSetting? effective = FeatureSetting.Effective(own, fallback);

            rows.Add(new AiFeatureResponse(
                feature.Code,
                feature.Label,
                feature.SourceModule,
                feature.DefaultProvider,
                own?.ModelId,
                effective?.ModelId,
                SourceOf(own, effective)));
        }

        return Result.Success<IReadOnlyCollection<AiFeatureResponse>>(rows);
    }

    private static AiFeatureTargetSource SourceOf(FeatureSetting? own, FeatureSetting? effective)
    {
        if (effective is null)
        {
            return AiFeatureTargetSource.Provider;
        }

        return own is null ? AiFeatureTargetSource.Default : AiFeatureTargetSource.Feature;
    }
}
