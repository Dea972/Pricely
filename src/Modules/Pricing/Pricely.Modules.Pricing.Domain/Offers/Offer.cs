using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Offers;

/// <summary>
/// A reference product sold by a merchant: the unit of the price comparison (product × merchant → price).
/// Creating an offer is the act of matching a merchant page with a reference product.
/// </summary>
public sealed class Offer : Entity
{
    public const int MerchantProductReferenceMaxLength = 100;

    private Offer()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid MerchantId { get; private set; }

    public Uri ProductPage { get; private set; }

    public string? MerchantProductReference { get; private set; }

    public MatchEvidence Evidence { get; private set; }

    public MatchStatus Status { get; private set; }

    public OfferConfirmationMethod? ConfirmationMethod { get; private set; }

    public DateTime ProposedOnUtc { get; private set; }

    public DateTime? ReviewedOnUtc { get; private set; }

    public bool IsComparable => Status == MatchStatus.Confirmed;

    /// <summary>
    /// Proposes a match. It is confirmed immediately when <see cref="OfferMatchingPolicy"/> finds a strong
    /// identifier, otherwise it waits for a review.
    /// </summary>
    public static Offer Propose(OfferCandidate candidate, ProductIdentity product, DateTime utcNow)
    {
        Offer offer = new()
        {
            Id = Guid.CreateVersion7(),
            ProductId = candidate.ProductId,
            MerchantId = candidate.MerchantId,
            ProductPage = candidate.ProductPage,
            MerchantProductReference = Truncate(candidate.MerchantProductReference),
            Evidence = candidate.Evidence,
            Status = MatchStatus.Proposed,
            ProposedOnUtc = utcNow
        };

        OfferConfirmationMethod? automaticConfirmation = OfferMatchingPolicy.Evaluate(product, candidate.Evidence);
        if (automaticConfirmation is null)
        {
            offer.Raise(new OfferProposedDomainEvent(offer.Id, offer.ProductId, offer.MerchantId));
        }
        else
        {
            offer.MarkAsConfirmed(automaticConfirmation.Value, utcNow);
        }

        return offer;
    }

    public Result Confirm(DateTime utcNow)
    {
        if (Status != MatchStatus.Proposed)
        {
            return Result.Failure(OfferErrors.NotPendingReview(Id, Status));
        }

        MarkAsConfirmed(OfferConfirmationMethod.Manual, utcNow);

        return Result.Success();
    }

    public Result Reject(DateTime utcNow)
    {
        if (Status != MatchStatus.Proposed)
        {
            return Result.Failure(OfferErrors.NotPendingReview(Id, Status));
        }

        Status = MatchStatus.Rejected;
        ReviewedOnUtc = utcNow;

        Raise(new OfferRejectedDomainEvent(Id, ProductId, MerchantId));

        return Result.Success();
    }

    private void MarkAsConfirmed(OfferConfirmationMethod method, DateTime utcNow)
    {
        Status = MatchStatus.Confirmed;
        ConfirmationMethod = method;
        ReviewedOnUtc = utcNow;

        Raise(new OfferConfirmedDomainEvent(Id, ProductId, MerchantId, method));
    }

    private static string? Truncate(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        string trimmed = reference.Trim();

        return trimmed.Length > MerchantProductReferenceMaxLength ? trimmed[..MerchantProductReferenceMaxLength] : trimmed;
    }
}
