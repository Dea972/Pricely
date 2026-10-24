using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;

namespace Pricely.Modules.Pricing.Infrastructure.Collection;

/// <summary>
/// Default <see cref="IOfferCollector"/> until a real collector (AI agent, scrapers, partner feeds) is plugged in.
/// It fails explicitly instead of pretending that no merchant sells the product.
/// </summary>
internal sealed class NotConfiguredOfferCollector : IOfferCollector
{
    private static readonly Error NotConfigured = Error.Problem(
        "Collection.CollectorNotConfigured",
        "No offer collector is configured. Register an IOfferCollector implementation in the Pricing module.");

    public Task<Result<IReadOnlyCollection<CollectedOffer>>> CollectAsync(
        OfferCollectionRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<IReadOnlyCollection<CollectedOffer>>(NotConfigured));
}
