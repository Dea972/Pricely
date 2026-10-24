using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Catalog.Application.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Brand,
    string? Gtin,
    string? ManufacturerReference,
    Guid ProductCategoryId) : ICommand;
