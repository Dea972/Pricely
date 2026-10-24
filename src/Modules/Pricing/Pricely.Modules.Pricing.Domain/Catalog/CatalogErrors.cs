using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Catalog;

public static class CatalogErrors
{
    public static Error ProductNotFound(Guid productId) =>
        Error.NotFound("Catalog.ProductNotFound", $"The product {productId} is unknown to the pricing module");

    public static Error CategoryNotFound(Guid productCategoryId) =>
        Error.NotFound("Catalog.CategoryNotFound", $"The product category {productCategoryId} is unknown to the pricing module");
}
