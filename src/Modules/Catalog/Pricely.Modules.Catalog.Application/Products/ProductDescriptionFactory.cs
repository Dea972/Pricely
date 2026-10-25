using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Application.Products;

internal static class ProductDescriptionFactory
{
    internal static Result<ProductDescription> Create(
        string name,
        string brand,
        string? gtin,
        string? manufacturerReference)
    {
        if (string.IsNullOrWhiteSpace(gtin))
        {
            return new ProductDescription(name, brand, null, manufacturerReference);
        }

        Result<Gtin> gtinResult = Gtin.Create(gtin);
        if (gtinResult.IsFailure)
        {
            return Result.Failure<ProductDescription>(gtinResult.Error);
        }

        return new ProductDescription(name, brand, gtinResult.Value, manufacturerReference);
    }
}
