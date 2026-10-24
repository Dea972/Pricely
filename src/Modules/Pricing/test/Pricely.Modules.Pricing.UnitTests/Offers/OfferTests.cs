using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.UnitTests.Abstractions;

namespace Pricely.Modules.Pricing.UnitTests.Offers;

public sealed class OfferTests : BaseTest
{
    private static readonly DateTime Now = new(2026, 10, 24, 18, 0, 0, DateTimeKind.Utc);
    private static readonly ProductIdentity Product = new("4006381333931", "Vicat", "CEM2-35");

    [Fact]
    public void Propose_ShouldConfirmAutomatically_WhenGtinMatches()
    {
        var offer = Offer.Propose(Candidate(new MatchEvidence("4006381333931", null, null, null)), Product, Now);

        offer.Status.Should().Be(MatchStatus.Confirmed);
        offer.ConfirmationMethod.Should().Be(OfferConfirmationMethod.Gtin);
        offer.IsComparable.Should().BeTrue();
        AssertDomainEventWasPublished<OfferConfirmedDomainEvent>(offer);
    }

    [Fact]
    public void Propose_ShouldWaitForReview_WhenEvidenceIsWeak()
    {
        var offer = Offer.Propose(Candidate(new MatchEvidence(null, "Vicat", null, 0.97m)), Product, Now);

        offer.Status.Should().Be(MatchStatus.Proposed);
        offer.ConfirmationMethod.Should().BeNull();
        offer.IsComparable.Should().BeFalse();
        AssertDomainEventWasPublished<OfferProposedDomainEvent>(offer);
    }

    [Fact]
    public void Confirm_ShouldConfirmManually_WhenProposed()
    {
        Offer offer = ProposedOffer();

        Result result = offer.Confirm(Now);

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(MatchStatus.Confirmed);
        offer.ConfirmationMethod.Should().Be(OfferConfirmationMethod.Manual);
        offer.ReviewedOnUtc.Should().Be(Now);
    }

    [Fact]
    public void Reject_ShouldRejectAndRaiseDomainEvent_WhenProposed()
    {
        Offer offer = ProposedOffer();
        offer.ClearDomainEvents();

        Result result = offer.Reject(Now);

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(MatchStatus.Rejected);
        AssertDomainEventWasPublished<OfferRejectedDomainEvent>(offer);
    }

    [Fact]
    public void Confirm_ShouldFail_WhenAlreadyReviewed()
    {
        Offer offer = ProposedOffer();
        offer.Reject(Now);

        Result result = offer.Confirm(Now);

        result.Error.Code.Should().Be("Offers.NotPendingReview");
    }

    [Fact]
    public void Propose_ShouldTruncateLongMerchantReference()
    {
        OfferCandidate candidate = Candidate(new MatchEvidence(null, null, null, null)) with
        {
            MerchantProductReference = new string('R', Offer.MerchantProductReferenceMaxLength + 20)
        };

        var offer = Offer.Propose(candidate, Product, Now);

        offer.MerchantProductReference.Should().HaveLength(Offer.MerchantProductReferenceMaxLength);
    }

    private static Offer ProposedOffer() =>
        Offer.Propose(Candidate(new MatchEvidence(null, null, null, 0.8m)), Product, Now);

    private static OfferCandidate Candidate(MatchEvidence evidence) =>
        new(Guid.NewGuid(), Guid.NewGuid(), new Uri("https://merchant.example/ciment-35kg"), "REF-1", evidence);
}
