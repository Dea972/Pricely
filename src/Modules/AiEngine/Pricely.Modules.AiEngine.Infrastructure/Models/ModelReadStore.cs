using System.Data.Common;
using Dapper;
using Pricely.Common.Infrastructure.Data;
using Pricely.Modules.AiEngine.Application.Models;
using Pricely.Modules.AiEngine.Application.Models.GetModels;

namespace Pricely.Modules.AiEngine.Infrastructure.Models;

/// <summary>
/// Reads the models with the price in force with SQL (Dapper): a lateral join on the price history.
/// </summary>
internal sealed class ModelReadStore(IDbConnectionFactory dbConnectionFactory) : IModelReadStore
{
    public async Task<IReadOnlyCollection<ModelResponse>> GetAllAsync(DateTime todayUtc, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 m.id AS {nameof(ModelResponse.Id)},
                 m.provider AS {nameof(ModelResponse.Provider)},
                 m.provider_model_id AS {nameof(ModelResponse.ProviderModelId)},
                 m.label AS {nameof(ModelResponse.Label)},
                 CAST(price.valid_from AS timestamp) AS {nameof(ModelResponse.PriceValidFrom)},
                 price.input_per_million AS {nameof(ModelResponse.InputPerMillion)},
                 price.cached_input_per_million AS {nameof(ModelResponse.CachedInputPerMillion)},
                 price.cache_write_per_million AS {nameof(ModelResponse.CacheWritePerMillion)},
                 price.output_per_million AS {nameof(ModelResponse.OutputPerMillion)}
             FROM aiengine.models m
             LEFT JOIN LATERAL (
                 SELECT p.valid_from, p.input_per_million, p.cached_input_per_million,
                              p.cache_write_per_million, p.output_per_million
                 FROM aiengine.model_prices p
                 WHERE p.model_id = m.id AND p.valid_from <= CAST(CAST(@Today AS timestamptz) AT TIME ZONE 'UTC' AS date)
                 ORDER BY p.valid_from DESC
                 LIMIT 1
             ) price ON TRUE
             ORDER BY m.provider, m.label
             """;

        return (await connection.QueryAsync<ModelResponse>(
            new CommandDefinition(sql, new { Today = todayUtc }, cancellationToken: cancellationToken))).AsList();
    }
}
