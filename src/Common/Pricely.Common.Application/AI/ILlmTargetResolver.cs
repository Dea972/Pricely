namespace Pricely.Common.Application.AI;

/// <summary>
/// Resolves the target configured for a feature. The default implementation returns no target (the feature's default
/// provider is used); the AiEngine module replaces it with the targets chosen by administrators.
/// </summary>
public interface ILlmTargetResolver
{
    Task<LlmTarget?> ResolveAsync(AiFeature feature, CancellationToken cancellationToken = default);
}
