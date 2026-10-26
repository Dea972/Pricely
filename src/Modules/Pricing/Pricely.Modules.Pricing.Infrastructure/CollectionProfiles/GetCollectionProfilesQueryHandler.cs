using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.CollectionProfiles.GetCollectionProfiles;

namespace Pricely.Modules.Pricing.Infrastructure.CollectionProfiles;

internal sealed class GetCollectionProfilesQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetCollectionProfilesQuery, IReadOnlyCollection<CollectionProfileSummaryResponse>>
{
    public async Task<Result<IReadOnlyCollection<CollectionProfileSummaryResponse>>> Handle(
        GetCollectionProfilesQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 p.product_category_id AS {nameof(CollectionProfileSummaryResponse.ProductCategoryId)},
                 c.name AS {nameof(CollectionProfileSummaryResponse.CategoryName)},
                 CAST((SELECT COUNT(*) FROM pricing.collection_profile_preferred_merchants m
                       WHERE m.collection_profile_id = p.id) AS integer) AS {nameof(CollectionProfileSummaryResponse.PreferredMerchantCount)},
                 p.instructions IS NOT NULL AS {nameof(CollectionProfileSummaryResponse.HasInstructions)}
             FROM pricing.collection_profiles p
             LEFT JOIN pricing.catalog_categories c ON c.id = p.product_category_id
             ORDER BY c.name
             """;

        IEnumerable<CollectionProfileSummaryResponse> profiles = await connection.QueryAsync<CollectionProfileSummaryResponse>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<CollectionProfileSummaryResponse>>(profiles.ToList());
    }
}
