using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

/// <summary>
/// Builds the collection request of a product: the reference product, the active preferred merchants
/// of its typology (inherited if needed) and the typology's instructions.
/// </summary>
public sealed class OfferCollectionPlanner(
    ICatalogProductRepository catalogProductRepository,
    ICatalogCategoryRepository catalogCategoryRepository,
    ICollectionProfileRepository collectionProfileRepository,
    IMerchantRepository merchantRepository)
{
    public async Task<Result<OfferCollectionPlan>> PlanAsync(Guid productId, CancellationToken cancellationToken)
    {
        CatalogProduct? product = await catalogProductRepository.GetAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<OfferCollectionPlan>(CatalogErrors.ProductNotFound(productId));
        }

        Result<ResolvedCollectionProfile> profile = await CollectionProfileLookup.ResolveAsync(
            product.ProductCategoryId,
            catalogCategoryRepository,
            collectionProfileRepository,
            cancellationToken);
        if (profile.IsFailure)
        {
            return Result.Failure<OfferCollectionPlan>(profile.Error);
        }

        List<MerchantToSearch> merchants = await GetActiveMerchantsAsync(profile.Value, cancellationToken);
        if (merchants.Count == 0)
        {
            return Result.Failure<OfferCollectionPlan>(
                CollectionProfileErrors.NoPreferredMerchants(product.ProductCategoryId));
        }

        ProductToCollect productToCollect = new(
            product.Id,
            product.Name,
            product.Brand,
            product.Gtin,
            product.ManufacturerReference);

        return new OfferCollectionPlan(
            new ProductIdentity(product.Gtin, product.Brand, product.ManufacturerReference),
            new OfferCollectionRequest(productToCollect, merchants, profile.Value.Instructions));
    }

    private async Task<List<MerchantToSearch>> GetActiveMerchantsAsync(
        ResolvedCollectionProfile profile,
        CancellationToken cancellationToken)
    {
        if (!profile.HasPreferredMerchants)
        {
            return [];
        }

        IReadOnlyCollection<Merchant> merchants =
            await merchantRepository.GetManyAsync(profile.PreferredMerchantIds, cancellationToken);
        var activeMerchants = merchants.Where(m => m.IsActive).ToDictionary(m => m.Id);

        return profile.PreferredMerchantIds
            .Where(activeMerchants.ContainsKey)
            .Select((merchantId, index) => new MerchantToSearch(
                merchantId,
                activeMerchants[merchantId].Name,
                activeMerchants[merchantId].Website,
                index + 1,
                activeMerchants[merchantId].SearchTemplate))
            .ToList();
    }
}
