using System.Globalization;
using System.Text;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.DevSeed;

/// <summary>
/// The collector of the test data: the page each merchant would show for a product on a given week (price, availability,
/// identifiers), from <see cref="SeedData"/>. The collection command then matches and records it like any collected offer.
/// </summary>
internal sealed class SeedOfferCollector : IOfferCollector
{
    /// <summary>The week being collected, 0 being the oldest.</summary>
    public int Week { get; set; }

    public DateTime ObservedOnUtc { get; set; }

    public Task<Result<IReadOnlyCollection<CollectedOffer>>> CollectAsync(
        OfferCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        SeedProduct? product = Array.Find(SeedData.Products, p => string.Equals(p.Gtin, request.Product.Gtin, StringComparison.Ordinal));
        if (product is null)
        {
            return Task.FromResult(Result.Success<IReadOnlyCollection<CollectedOffer>>([]));
        }

        List<CollectedOffer> offers = [];
        foreach (MerchantToSearch target in request.Merchants)
        {
            SeedMerchant? merchant = Array.Find(SeedData.Merchants, m => string.Equals(m.Name, target.Name, StringComparison.OrdinalIgnoreCase));
            if (merchant is not null)
            {
                offers.Add(Offer(product, merchant, target));
            }
        }

        return Task.FromResult(Result.Success<IReadOnlyCollection<CollectedOffer>>(offers));
    }

    private CollectedOffer Offer(SeedProduct product, SeedMerchant merchant, MerchantToSearch target)
    {
        SeedListing? listing = Array.Find(
            SeedData.Listings,
            l => string.Equals(l.Gtin, product.Gtin, StringComparison.Ordinal) && string.Equals(l.MerchantKey, merchant.Key, StringComparison.Ordinal));

        bool isLastWeek = Week == SeedData.Weeks - 1;
        bool showsPack = isLastWeek && SeedData.PackPrice == (product.Gtin, merchant.Key);
        bool isOutOfStock = Week >= SeedData.Weeks - 2 && SeedData.OutOfStock == (product.Gtin, merchant.Key);

        return new CollectedOffer
        {
            MerchantId = target.MerchantId,
            ProductPage = new Uri(target.Website, "p/" + Slug(product.Name)),
            MerchantProductReference = $"{merchant.Key.ToUpperInvariant()}-{product.Gtin[^6..]}",
            PriceAmount = showsPack ? Price(product, merchant) * 10 : Price(product, merchant),
            Currency = "EUR",
            Availability = isOutOfStock ? Availability.OutOfStock : Availability.InStock,
            ObservedOnUtc = ObservedOnUtc,
            Evidence = Evidence(product, listing?.Match ?? SeedMatch.Gtin, listing)
        };
    }

    /// <summary>A few cents up or down from one week to the next, never the same pattern for two merchants.</summary>
    private decimal Price(SeedProduct product, SeedMerchant merchant)
    {
        int merchantIndex = Array.IndexOf(SeedData.Merchants, merchant);
        double variation = 0.03 * Math.Sin(Week * 0.9 + merchantIndex * 1.7 + product.Gtin[^2]);

        return decimal.Round(product.Price * merchant.PriceFactor * (decimal)(1 + variation), 2);
    }

    /// <summary>
    /// What the scripted merchant page shows. By default the page shows the identifiers of the product (that is what a
    /// real merchant page with the same GTIN looks like): the matching policy, not this collector, confirms the offer.
    /// </summary>
    private static MatchEvidence Evidence(SeedProduct product, SeedMatch match, SeedListing? listing) =>
        match switch
        {
            SeedMatch.Gtin => new MatchEvidence(product.Gtin, product.Brand, null, null) { ObservedTitle = product.Name },
            SeedMatch.BrandAndReference => new MatchEvidence(null, product.Brand, product.Reference, null) { ObservedTitle = product.Name },
            SeedMatch.OtherGtin => Reviewed(listing!, gtin: listing!.ObservedValue, brand: product.Brand, reference: null),
            SeedMatch.OtherReference => Reviewed(listing!, gtin: null, brand: product.Brand, reference: listing!.ObservedValue),
            _ => Reviewed(listing!, gtin: null, brand: product.Brand, reference: null)
        };

    private static MatchEvidence Reviewed(SeedListing listing, string? gtin, string? brand, string? reference) =>
        new(gtin, brand, reference, listing.Similarity)
        {
            ObservedTitle = listing.ObservedTitle,
            AiRationale = listing.Rationale
        };

    private static string Slug(string name)
    {
        var slug = new StringBuilder();
        foreach (char character in name.Normalize(NormalizationForm.FormD))
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                slug.Append(char.ToLower(character, CultureInfo.InvariantCulture));
            }
            else if (slug.Length > 0 && slug[^1] != '-' && CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                slug.Append('-');
            }
        }

        return slug.ToString().TrimEnd('-');
    }
}
