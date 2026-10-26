using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.AiEngine.Application.Models.ProposeModelPrices;

/// <summary>
/// Looks up the current price of every model of the catalogue (LiteLLM, else the AI on the pricing page of the provider)
/// and proposes it next to the price in force (ADR-021). Nothing is saved: the user applies a proposal
/// (<c>SetModelPriceCommand</c> with its source).
/// </summary>
public sealed record ProposeModelPricesCommand : ICommand<IReadOnlyCollection<ModelPriceProposalResponse>>;
