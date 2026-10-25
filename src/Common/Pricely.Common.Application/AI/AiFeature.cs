namespace Pricely.Common.Application.AI;

/// <summary>
/// A feature of a module that calls an LLM. Declared in code by the module (<see cref="AiFeatureServiceCollectionExtensions.AddAiFeature"/>),
/// its target (provider and model) can be changed at runtime by the AiEngine module.
/// </summary>
/// <param name="Code">Unique code, <c>"&lt;module&gt;.&lt;usage&gt;"</c> (for example <c>pricing.offer-collection</c>).</param>
/// <param name="Label">Label shown to administrators.</param>
/// <param name="SourceModule">Module that declares the feature.</param>
/// <param name="DefaultProvider">Provider used when no target is configured (see <see cref="AiProviderKeys"/>).</param>
public sealed record AiFeature(string Code, string Label, string SourceModule, string DefaultProvider);
