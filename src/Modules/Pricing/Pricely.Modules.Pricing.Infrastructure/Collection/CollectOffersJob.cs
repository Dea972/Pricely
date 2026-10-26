using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;
using Pricely.Modules.Pricing.Application.Offers.GetProductsToCollect;
using Quartz;

namespace Pricely.Modules.Pricing.Infrastructure.Collection;

/// <summary>
/// Periodically collects the offers of every known product. Each product runs in its own scope,
/// so a failure on one product does not stop the others.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class CollectOffersJob(IServiceScopeFactory serviceScopeFactory, ILogger<CollectOffersJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        IReadOnlyCollection<Guid> productIds = await GetProductIdsAsync(context.CancellationToken);

        logger.LogInformation("Pricing - Collecting offers for {Count} products", productIds.Count);

        foreach (Guid productId in productIds)
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

            Result<CollectProductOffersResponse> result = await sender.Send(
                new CollectProductOffersCommand(productId),
                context.CancellationToken);

            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Pricing - Offer collection failed for product {ProductId}: {ErrorCode} {ErrorDescription}",
                    productId,
                    result.Error.Code,
                    result.Error.Description);
            }
        }
    }

    private async Task<IReadOnlyCollection<Guid>> GetProductIdsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result<IReadOnlyCollection<Guid>> productIds = await sender.Send(new GetProductsToCollectQuery(), cancellationToken);
        if (productIds.IsFailure)
        {
            logger.LogWarning(
                "Pricing - Cannot read the products of Catalog: {ErrorCode} {ErrorDescription}",
                productIds.Error.Code,
                productIds.Error.Description);

            return [];
        }

        return productIds.Value;
    }
}
