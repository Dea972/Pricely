using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;

/// <summary>
/// Turns a submitted offer into a <see cref="CollectedOffer"/> only if its price appears in the page snapshot it cites
/// (ADR-009): a price the collector did not actually read is refused. The page and the observation time are the ones
/// of the snapshot, never the collector's.
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

        if (!snapshot.ShowsPrice(offer.Price))
        {
            return Result.Failure<CollectedOffer>(PageSnapshotErrors.PriceNotShown(offer.Price));
        }

        return new CollectedOffer
        {
            MerchantId = snapshot.MerchantId,
            ProductPage = snapshot.Address,
            MerchantProductReference = Cut(offer.MerchantProductReference, 100),
            PriceAmount = offer.Price,
            Currency = offer.Currency.Trim().ToUpperInvariant(),
            Availability = offer.Availability,
            ObservedOnUtc = snapshot.FetchedOnUtc,
            Evidence = new MatchEvidence(
                Cut(offer.ObservedGtin, 14),
                Cut(offer.ObservedBrand, 150),
                Cut(offer.ObservedManufacturerReference, 100),
                offer.Similarity is decimal similarity ? Math.Clamp(similarity, 0, 1) : null)
            {
                ObservedTitle = Cut(offer.ObservedTitle, MatchEvidence.ObservedTitleMaxLength),
                AiRationale = Cut(offer.Rationale, MatchEvidence.AiRationaleMaxLength)
            }
        };
    }

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
