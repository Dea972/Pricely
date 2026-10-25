using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Domain.Features;

/// <summary>
/// Model chosen by an administrator for an AI feature. Features are declared in code (<c>AiFeature</c>); a setting only
/// exists once a target has been chosen. The <see cref="DefaultCode"/> setting targets features without their own.
/// </summary>
public sealed class FeatureSetting : Entity
{
    public const string DefaultCode = "default";

    public const int FeatureCodeMaxLength = 100;

    private FeatureSetting()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Code of the feature (<c>AiFeature.Code</c>), or <see cref="DefaultCode"/>.</summary>
    public string FeatureCode { get; private set; }

    public Guid ModelId { get; private set; }

    public DateTime UpdatedOnUtc { get; private set; }

    public bool IsDefault => IsDefaultCode(FeatureCode);

    public static bool IsDefaultCode(string featureCode) =>
        string.Equals(featureCode, DefaultCode, StringComparison.OrdinalIgnoreCase);

    public static Result<FeatureSetting> Create(string featureCode, Guid modelId, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(featureCode) || featureCode.Trim().Length > FeatureCodeMaxLength)
        {
            return Result.Failure<FeatureSetting>(FeatureSettingErrors.InvalidFeatureCode);
        }

        return new FeatureSetting
        {
            Id = Guid.CreateVersion7(),
            FeatureCode = featureCode.Trim(),
            ModelId = modelId,
            UpdatedOnUtc = utcNow
        };
    }

    public void Target(Guid modelId, DateTime utcNow)
    {
        ModelId = modelId;
        UpdatedOnUtc = utcNow;
    }

    /// <summary>
    /// Setting that applies to a feature: its own, otherwise the default one.
    /// </summary>
    public static FeatureSetting? Effective(FeatureSetting? own, FeatureSetting? fallback) => own ?? fallback;
}
