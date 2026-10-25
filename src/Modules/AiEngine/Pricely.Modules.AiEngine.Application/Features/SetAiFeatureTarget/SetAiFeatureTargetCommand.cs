using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.AiEngine.Application.Features.SetAiFeatureTarget;

/// <summary>Targets a model for a feature, or for every feature without its own target (code <c>default</c>).</summary>
public sealed record SetAiFeatureTargetCommand(string FeatureCode, Guid ModelId) : ICommand;
