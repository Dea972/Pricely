using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Merchants.SuggestMerchant;

/// <summary>
/// Reads the site of a merchant to come and proposes its setup (ADR-020): nothing is saved, the user validates and
/// registers it. A command, not a query: it reads the web and calls a model.
/// </summary>
/// <param name="Website">Address of the site (<c>https://</c> is added when missing).</param>
/// <param name="Name">The name the user already has in mind, if any.</param>
public sealed record SuggestMerchantCommand(string Website, string? Name) : ICommand<MerchantSuggestionResponse>;
