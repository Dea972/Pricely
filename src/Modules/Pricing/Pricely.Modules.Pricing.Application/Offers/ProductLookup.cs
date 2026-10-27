using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Products;

namespace Pricely.Modules.Pricing.Application.Offers;

/// <summary>Reads from Catalog the products that rows of Pricing refer to, by batches within the limit of the contract.</summary>
internal static class ProductLookup
{
    internal static async Task<Result<Dictionary<Guid, ProductSummary>>> LoadAsync(
        IProductReader productReader,
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var products = new Dictionary<Guid, ProductSummary>();

        foreach (Guid[] batch in productIds.Distinct().Chunk(IProductReader.MaxIds))
        {
            Result<IReadOnlyList<ProductSummary>> read = await productReader.GetByIdsAsync(batch, cancellationToken);
            if (read.IsFailure)
            {
                return Result.Failure<Dictionary<Guid, ProductSummary>>(read.Error);
            }

            foreach (ProductSummary product in read.Value)
            {
                products[product.Id] = product;
            }
        }

        return products;
    }
}
