using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Domain.Features;

public static class FeatureSettingErrors
{
    public static readonly Error InvalidFeatureCode = Error.Problem(
        "FeatureSettings.InvalidFeatureCode",
        $"The feature code is required and cannot exceed {FeatureSetting.FeatureCodeMaxLength} characters");

    public static Error UnknownFeature(string featureCode) =>
        Error.NotFound("FeatureSettings.UnknownFeature", $"No module declares the AI feature '{featureCode}'");

    public static Error NotFound(string featureCode) =>
        Error.NotFound("FeatureSettings.NotFound", $"No target is configured for the AI feature '{featureCode}'");
}
