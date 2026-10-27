using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;
using Pricely.Modules.Pricing.Application.PageSnapshots;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

/// <summary>
/// Opens the product page of a merchant by the GTIN of the reference product and reads the offer from the structured
/// data of the page, without the model (ADR-026). It decides only when the page declares a product with the same GTIN
/// and a price; otherwise (no template, no GTIN, page unreadable or without that product) it returns null and the
/// collection agent searches as usual. The offer goes through the same verification as the agent's.
/// </summary>
public sealed class ProductPageShortcut(MerchantPageReader pageReader, SubmittedOfferVerifier verifier)
{
    public async Task<CollectedOffer?> TryCollectAsync(
        ProductToCollect product,
        MerchantToSearch merchant,
        CancellationToken cancellationToken)
    {
        Uri? address = Merchant.ProductPageAddress(merchant.ProductPageTemplate, product.Gtin);
        if (address is null)
        {
            return null;
        }

        Result<MerchantPage> page = await pageReader.ReadPageAsync(merchant.MerchantId, address, searchText: null, cancellationToken);
        if (page.IsFailure)
        {
            return null;
        }

        StructuredProduct? declared = page.Value.Products.FirstOrDefault(p => OfferMatchingPolicy.HaveSameGtin(product.Gtin, p.Gtin));

        // The offer of the page first (the merchant's, or the marketplace seller it puts forward), a bound of several
        // sellers only when there is no other.
        if (declared is null || declared.Offers.Count == 0)
        {
            return null;
        }

        StructuredOffer offer = declared.Offers.FirstOrDefault(o => !o.IsAggregate) ?? declared.Offers[0];

        // A price without its currency is left to the agent: the shortcut never assumes one.
        if (string.IsNullOrWhiteSpace(offer.Currency))
        {
            return null;
        }

        Result<CollectedOffer> collected = await verifier.VerifyAsync(
            new SubmittedOffer
            {
                SnapshotId = page.Value.Snapshot.Id,
                Price = offer.Price,
                Currency = offer.Currency,
                Availability = AvailabilityOf(offer.Availability),
                MerchantProductReference = declared.Sku,
                ObservedTitle = declared.Name,
                ObservedGtin = declared.Gtin,
                ObservedBrand = declared.Brand,
                Rationale = offer.Seller is null
                    ? "Page produit ouverte par le GTIN ; prix lu dans les données structurées de la page."
                    : $"Page produit ouverte par le GTIN ; prix lu dans les données structurées de la page, vendu par {offer.Seller}."
            },
            cancellationToken);

        return collected.IsSuccess ? collected.Value : null;
    }

    /// <summary>schema.org availability (<c>InStock</c>, <c>LimitedAvailability</c>, <c>OutOfStock</c>, <c>SoldOut</c>...).</summary>
    private static Availability AvailabilityOf(string? availability) =>
        availability?.ToUpperInvariant() switch
        {
            "INSTOCK" or "LIMITEDAVAILABILITY" or "ONLINEONLY" or "INSTOREONLY" => Availability.InStock,
            "OUTOFSTOCK" or "SOLDOUT" or "DISCONTINUED" => Availability.OutOfStock,
            _ => Availability.Unknown
        };
}
