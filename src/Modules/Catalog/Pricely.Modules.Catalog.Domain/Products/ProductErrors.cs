using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Domain.Products;

public static class ProductErrors
{
    public static readonly Error InvalidName = Error.Problem(
        "Products.InvalidName",
        $"The product name is required and cannot exceed {Product.NameMaxLength} characters");

    public static readonly Error InvalidBrand = Error.Problem(
        "Products.InvalidBrand",
        $"The brand is required and cannot exceed {Product.BrandMaxLength} characters");

    public static readonly Error InvalidManufacturerReference = Error.Problem(
        "Products.InvalidManufacturerReference",
        $"The manufacturer reference cannot exceed {Product.ManufacturerReferenceMaxLength} characters");

    public static Error NotFound(Guid productId) =>
        Error.NotFound("Products.NotFound", $"The product with the identifier {productId} was not found");

    public static Error InvalidGtin(string value) =>
        Error.Problem("Products.InvalidGtin", $"'{value}' is not a valid GTIN");

    public static Error GtinAlreadyUsed(string gtin) =>
        Error.Conflict("Products.GtinAlreadyUsed", $"A product with the GTIN {gtin} already exists");
}
