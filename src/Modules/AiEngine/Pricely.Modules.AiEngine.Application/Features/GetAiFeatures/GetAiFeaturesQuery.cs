using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.AiEngine.Application.Features.GetAiFeatures;

/// <summary>
/// The AI features declared by the modules, with the model each one actually calls. First row: the default target.
/// </summary>
public sealed record GetAiFeaturesQuery : IQuery<IReadOnlyCollection<AiFeatureResponse>>;
