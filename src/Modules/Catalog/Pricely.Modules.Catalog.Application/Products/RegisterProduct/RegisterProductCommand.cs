using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Catalog.Application.Products.RegisterProduct;

public sealed record RegisterProductCommand(
    string Name,
    string Brand,
    string? Gtin,
    string? ManufacturerReference,
    Guid ProductCategoryId) : ICommand<Guid>;
