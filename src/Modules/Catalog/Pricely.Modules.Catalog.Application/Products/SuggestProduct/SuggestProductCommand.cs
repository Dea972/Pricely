using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Catalog.Application.Products.SuggestProduct;

/// <summary>
/// Reads a product page (manufacturer or merchant) and proposes the reference product (ADR-020): nothing is saved, the
/// user checks the fields and registers it. A command, not a query: it reads the web and calls a model.
/// </summary>
public sealed record SuggestProductCommand(Uri ProductPage) : ICommand<ProductSuggestionResponse>;
