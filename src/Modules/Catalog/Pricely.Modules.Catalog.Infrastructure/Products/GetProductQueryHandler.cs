using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Products.GetProduct;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Infrastructure.Products;

internal sealed class GetProductQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetProductQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(ProductResponse.Id)},
                 name AS {nameof(ProductResponse.Name)},
                 brand AS {nameof(ProductResponse.Brand)},
                 gtin AS {nameof(ProductResponse.Gtin)},
                 manufacturer_reference AS {nameof(ProductResponse.ManufacturerReference)},
                 product_category_id AS {nameof(ProductResponse.ProductCategoryId)}
             FROM catalog.products
             WHERE id = @ProductId
             """;

        ProductResponse? product = await connection.QuerySingleOrDefaultAsync<ProductResponse>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));

        if (product is null)
        {
            return Result.Failure<ProductResponse>(ProductErrors.NotFound(request.ProductId));
        }

        return product;
    }
}
