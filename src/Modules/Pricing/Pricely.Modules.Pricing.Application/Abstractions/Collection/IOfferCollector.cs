using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Application.Abstractions.Collection;

/// <summary>
/// Port to the outside world: finds the merchant pages selling a product and reads their price.
/// Implementations (AI agent, dedicated scrapers, partner feeds) live in Infrastructure and act as an
/// anti-corruption layer: they return normalized <see cref="CollectedOffer"/>s, never raw pages.
/// </summary>
public interface IOfferCollector
{
    Task<Result<IReadOnlyCollection<CollectedOffer>>> CollectAsync(
        OfferCollectionRequest request,
        CancellationToken cancellationToken = default);
}
