using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.AiEngine.Application.Models.GetModels;

/// <summary>The model catalog with the price in force today (UTC).</summary>
public sealed record GetModelsQuery : IQuery<IReadOnlyCollection<ModelResponse>>;
