using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Categories.GetProductCategories;

namespace Pricely.Modules.Catalog.Infrastructure.Categories;

internal sealed class GetProductCategoriesQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetProductCategoriesQuery, IReadOnlyCollection<ProductCategoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<ProductCategoryResponse>>> Handle(
        GetProductCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(ProductCategoryResponse.Id)},
                 name AS {nameof(ProductCategoryResponse.Name)},
                 parent_id AS {nameof(ProductCategoryResponse.ParentId)},
                 depth AS {nameof(ProductCategoryResponse.Depth)}
             FROM catalog.product_categories
             ORDER BY depth, name
             """;

        IEnumerable<ProductCategoryResponse> categories = await connection.QueryAsync<ProductCategoryResponse>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<ProductCategoryResponse>>(categories.ToList());
    }
}
