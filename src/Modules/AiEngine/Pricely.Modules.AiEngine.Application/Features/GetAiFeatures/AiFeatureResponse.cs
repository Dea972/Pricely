namespace Pricely.Modules.AiEngine.Application.Features.GetAiFeatures;

/// <summary>Where what a feature calls comes from.</summary>
public enum AiFeatureTargetSource
{
    /// <summary>Its own target.</summary>
    Feature = 0,

    /// <summary>The default target.</summary>
    Default = 1,

    /// <summary>No target configured: the feature's default provider with its default model.</summary>
    Provider = 2
}

/// <param name="Code">Feature code, or <c>default</c> for the default target row.</param>
/// <param name="OwnModelId">Model targeted by the feature itself, if any.</param>
/// <param name="EffectiveModelId">Model actually called, if a target applies.</param>
public sealed record AiFeatureResponse(
    string Code,
    string Label,
    string? SourceModule,
    string? DefaultProvider,
    Guid? OwnModelId,
    Guid? EffectiveModelId,
    AiFeatureTargetSource Source);
