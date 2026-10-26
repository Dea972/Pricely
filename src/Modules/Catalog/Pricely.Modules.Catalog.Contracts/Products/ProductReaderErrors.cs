using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Contracts.Products;

/// <summary>
/// Errors of <see cref="IProductReader"/>.
/// </summary>
public static class ProductReaderErrors
{
    public static readonly Error TooManyIds = Error.Failure(
        "ProductReader.TooManyIds",
        $"At most {IProductReader.MaxIds} product identifiers per read.");

    public static readonly Error InvalidPageSize = Error.Failure(
        "ProductReader.InvalidPageSize",
        $"The page size must be between 1 and {IProductReader.MaxPageSize}.");
}
