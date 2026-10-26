using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;

/// <summary>
/// Turns a submitted offer into a <see cref="CollectedOffer"/> only if its price appears in the product page snapshot it
/// cites (ADR-009, ADR-017): a price the collector did not actually read is refused. The page and the observation time
/// are the ones of the snapshot, never the collector's. An identifier (GTIN, brand, reference) that the page does not
/// show is dropped: the collector knows the identifiers of the reference product, and <see cref="OfferMatchingPolicy"/>
/// confirms on them, so keeping one it did not read would let it confirm the offer itself.
/// </summary>
public sealed class SubmittedOfferVerifier(IPageSnapshotRepository pageSnapshotRepository)
{
    public async Task<Result<CollectedOffer>> VerifyAsync(SubmittedOffer offer, CancellationToken cancellationToken)
    {
        PageSnapshot? snapshot = await pageSnapshotRepository.GetAsync(offer.SnapshotId, cancellationToken);
        if (snapshot is null)
        {
            return Result.Failure<CollectedOffer>(PageSnapshotErrors.NotFound(offer.SnapshotId));
        }

        if (snapshot.IsSearchPage)
        {
            return Result.Failure<CollectedOffer>(PageSnapshotErrors.SearchPage(snapshot.Id));
        }

        if (!snapshot.ShowsPrice(offer.Price))
        {
            return Result.Failure<CollectedOffer>(PageSnapshotErrors.PriceNotShown(offer.Price));
        }

        return new CollectedOffer
        {
            MerchantId = snapshot.MerchantId,
            ProductPage = snapshot.Address,
            MerchantProductReference = Cut(offer.MerchantProductReference, Offer.MerchantProductReferenceMaxLength),
            PriceAmount = offer.Price,
            Currency = offer.Currency.Trim().ToUpperInvariant(),
            Availability = offer.Availability,
            ObservedOnUtc = snapshot.FetchedOnUtc,
            Evidence = new MatchEvidence(
                Shown(snapshot, offer.ObservedGtin, MatchEvidence.ObservedGtinMaxLength),
                Shown(snapshot, offer.ObservedBrand, MatchEvidence.ObservedBrandMaxLength),
                Shown(snapshot, offer.ObservedManufacturerReference, MatchEvidence.ObservedManufacturerReferenceMaxLength),
                offer.Similarity is decimal similarity ? Math.Clamp(similarity, 0, 1) : null)
            {
                ObservedTitle = Cut(offer.ObservedTitle, MatchEvidence.ObservedTitleMaxLength),
                AiRationale = Cut(offer.Rationale, MatchEvidence.AiRationaleMaxLength)
            }
        };
    }

    private static string? Shown(PageSnapshot snapshot, string? identifier, int maxLength) =>
        snapshot.ShowsIdentifier(identifier) ? Cut(identifier, maxLength) : null;

    private static string? Cut(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
