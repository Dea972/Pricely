using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

public sealed record OfferCollectionPlan(ProductIdentity ProductIdentity, OfferCollectionRequest Request);
