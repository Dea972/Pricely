namespace Pricely.Common.Application.AI;

/// <summary>
/// Single entry point to LLMs: gives a feature the client of its target. Modules never inject a provider directly.
/// </summary>
public interface ILlmGateway
{
    Task<LlmClient> ForFeatureAsync(AiFeature feature, CancellationToken cancellationToken = default);
}
