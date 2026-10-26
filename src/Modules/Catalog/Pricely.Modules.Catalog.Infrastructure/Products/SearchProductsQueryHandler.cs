using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Products.GetProduct;
using Pricely.Modules.Catalog.Application.Products.SearchProducts;

namespace Pricely.Modules.Catalog.Infrastructure.Products;

internal sealed class SearchProductsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<SearchProductsQuery, IReadOnlyCollection<ProductResponse>>
{
    public async Task<Result<IReadOnlyCollection<ProductResponse>>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
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
             WHERE (CAST(@Pattern AS text) IS NULL
                    OR name ILIKE @Pattern ESCAPE '\'
                    OR brand ILIKE @Pattern ESCAPE '\'
                    OR manufacturer_reference ILIKE @Pattern ESCAPE '\'
                    OR gtin = @Text)
               AND (CAST(@ProductCategoryId AS uuid) IS NULL OR product_category_id = @ProductCategoryId)
             ORDER BY name
             LIMIT @Limit
             """;

        string? text = string.IsNullOrWhiteSpace(request.Text) ? null : request.Text.Trim();

        IEnumerable<ProductResponse> products = await connection.QueryAsync<ProductResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    request.Limit,
                    Text = text,
                    Pattern = text is null ? null : $"%{EscapeLikePattern(text)}%",
                    request.ProductCategoryId
                },
                cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<ProductResponse>>(products.ToList());
    }

    private static string EscapeLikePattern(string text) =>
        text.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal)
            .Replace("[", @"\[", StringComparison.Ordinal);
}
