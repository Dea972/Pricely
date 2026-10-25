using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Domain.Features;

public static class FeatureSettingErrors
{
    public static Error UnknownFeature(string featureCode) =>
        Error.NotFound("FeatureSettings.UnknownFeature", $"No module declares the AI feature '{featureCode}'");

    public static Error NotFound(string featureCode) =>
        Error.NotFound("FeatureSettings.NotFound", $"No target is configured for the AI feature '{featureCode}'");
}
