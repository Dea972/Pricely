namespace Pricely.Common.Application.AI;

/// <summary>
/// What a feature calls: a provider (see <see cref="AiProviderKeys"/>) and, optionally, a model of this provider.
/// Without a model, the provider's configured default model is used.
/// </summary>
public sealed record LlmTarget(string Provider, string? Model = null);
