using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Products;

namespace Pricely.Modules.Pricing.Application.Offers.GetProductsToCollect;

/// <summary>Walks the products of Catalog page by page (cursor) through its contract.</summary>
internal sealed class GetProductsToCollectQueryHandler(IProductReader productReader)
    : IQueryHandler<GetProductsToCollectQuery, IReadOnlyCollection<Guid>>
{
    public async Task<Result<IReadOnlyCollection<Guid>>> Handle(GetProductsToCollectQuery request, CancellationToken cancellationToken)
    {
        List<Guid> productIds = [];
        Guid? after = null;

        while (true)
        {
            Result<IReadOnlyList<Guid>> page = await productReader.GetIdsPageAsync(after, IProductReader.MaxPageSize, cancellationToken);
            if (page.IsFailure)
            {
                return Result.Failure<IReadOnlyCollection<Guid>>(page.Error);
            }

            productIds.AddRange(page.Value);
            if (page.Value.Count < IProductReader.MaxPageSize)
            {
                return productIds;
            }

            after = page.Value[^1];
        }
    }
}
