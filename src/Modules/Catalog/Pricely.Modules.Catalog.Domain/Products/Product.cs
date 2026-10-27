using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Domain.Products;

/// <summary>
/// Reference product: what is being compared across merchants.
/// </summary>
public sealed class Product : Entity
{
    public const int NameMaxLength = 300;

    public const int BrandMaxLength = 150;

    public const int ManufacturerReferenceMaxLength = 100;

    private Product()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Brand { get; private set; }

    public Gtin? Gtin { get; private set; }

    public string? ManufacturerReference { get; private set; }

    public Guid ProductCategoryId { get; private set; }

    public static Result<Product> Register(ProductDescription description, Guid productCategoryId)
    {
        Result validation = Validate(description);
        if (validation.IsFailure)
        {
            return Result.Failure<Product>(validation.Error);
        }

        Product product = new()
        {
            Id = Guid.CreateVersion7(),
            ProductCategoryId = productCategoryId
        };

        product.Apply(description);

        product.Raise(new ProductRegisteredDomainEvent(product.Id));

        return product;
    }

    public Result Update(ProductDescription description, Guid productCategoryId)
    {
        Result validation = Validate(description);
        if (validation.IsFailure)
        {
            return validation;
        }

        Apply(description);
        ProductCategoryId = productCategoryId;

        Raise(new ProductUpdatedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Whether the product has this manufacturer reference, written with or without spaces, dashes, dots or case
    /// (« CR-35 » is « cr 35 »).
    /// </summary>
    public bool HasManufacturerReference(string? reference) =>
        Code(reference).Length > 0 && string.Equals(Code(ManufacturerReference), Code(reference), StringComparison.Ordinal);

    private static string Code(string? value) =>
        value is null ? string.Empty : new string([.. value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant)]);

    private void Apply(ProductDescription description)
    {
        Name = description.Name.Trim();
        Brand = description.Brand.Trim();
        Gtin = description.Gtin;
        ManufacturerReference = string.IsNullOrWhiteSpace(description.ManufacturerReference)
            ? null
            : description.ManufacturerReference.Trim();
    }

    private static Result Validate(ProductDescription description)
    {
        if (string.IsNullOrWhiteSpace(description.Name) || description.Name.Trim().Length > NameMaxLength)
        {
            return Result.Failure(ProductErrors.InvalidName);
        }

        if (string.IsNullOrWhiteSpace(description.Brand) || description.Brand.Trim().Length > BrandMaxLength)
        {
            return Result.Failure(ProductErrors.InvalidBrand);
        }

        if (description.ManufacturerReference?.Trim().Length > ManufacturerReferenceMaxLength)
        {
            return Result.Failure(ProductErrors.InvalidManufacturerReference);
        }

        return Result.Success();
    }
}
