using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.WebResearch.SearchWeb;

/// <summary>
/// Searches the web (ADR-027) for a product, a price or a merchant: pages and extracts, never a verified price.
/// </summary>
public sealed record SearchWebQuery(string Query) : IQuery<WebSearchResponse>;
