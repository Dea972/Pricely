using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.AiEngine.Application.Features.ResetAiFeatureTarget;

/// <summary>Removes the target of a feature: it falls back to the default target, then to its default provider.</summary>
public sealed record ResetAiFeatureTargetCommand(string FeatureCode) : ICommand;
