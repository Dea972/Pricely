using Microsoft.Extensions.DependencyInjection;
using Pricely.Common.Application.AI;

namespace Pricely.Common.Infrastructure.Ai;

/// <summary>
/// Gives a feature the LLM service of its target: the one configured in AiEngine (<see cref="ILlmTargetResolver"/>),
/// otherwise the feature's default provider with the provider's default model.
/// </summary>
internal sealed class LlmGateway(IServiceProvider serviceProvider, ILlmTargetResolver targetResolver) : ILlmGateway
{
    public async Task<LlmClient> ForFeatureAsync(AiFeature feature, CancellationToken cancellationToken = default)
    {
        LlmTarget target = await targetResolver.ResolveAsync(feature, cancellationToken) ?? new LlmTarget(feature.DefaultProvider);

        LlmServiceBase service = serviceProvider.GetRequiredKeyedService<LlmServiceBase>(target.Provider);

        return new LlmClient(service, feature, target);
    }
}

/// <summary>
/// Used when the AiEngine module is not loaded: every feature keeps its default provider and model.
/// </summary>
internal sealed class DefaultLlmTargetResolver : ILlmTargetResolver
{
    public Task<LlmTarget?> ResolveAsync(AiFeature feature, CancellationToken cancellationToken = default) =>
        Task.FromResult<LlmTarget?>(null);
}
