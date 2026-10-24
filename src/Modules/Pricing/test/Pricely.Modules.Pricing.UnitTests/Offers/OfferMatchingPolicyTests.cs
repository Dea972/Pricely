using FluentAssertions;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.UnitTests.Offers;

public sealed class OfferMatchingPolicyTests
{
    private static readonly ProductIdentity Product = new("4006381333931", "Vicat", "CEM II/B-LL 32,5");

    [Fact]
    public void Evaluate_ShouldConfirmByGtin_WhenGtinsMatch()
    {
        MatchEvidence evidence = new("4006381333931", null, null, null);

        OfferMatchingPolicy.Evaluate(Product, evidence).Should().Be(OfferConfirmationMethod.Gtin);
    }

    [Fact]
    public void Evaluate_ShouldCompareGtinsOnFourteenDigits()
    {
        MatchEvidence evidence = new("0 4006381 333931", null, null, null);

        OfferMatchingPolicy.Evaluate(Product, evidence).Should().Be(OfferConfirmationMethod.Gtin);
    }

    [Fact]
    public void Evaluate_ShouldConfirmByManufacturerReference_WhenBrandAndReferenceMatch()
    {
        MatchEvidence evidence = new(null, "VICAT", "cem ii b ll 325", 0.4m);

        OfferMatchingPolicy.Evaluate(Product, evidence).Should().Be(OfferConfirmationMethod.ManufacturerReference);
    }

    [Fact]
    public void Evaluate_ShouldRequireReview_WhenOnlyTheReferenceMatches()
    {
        MatchEvidence evidence = new(null, "Lafarge", "CEM II/B-LL 32,5", 0.99m);

        OfferMatchingPolicy.Evaluate(Product, evidence).Should().BeNull();
    }

    [Fact]
    public void Evaluate_ShouldRequireReview_WhenOnlyAiSimilarityIsHigh()
    {
        MatchEvidence evidence = new(null, null, null, 0.99m);

        OfferMatchingPolicy.Evaluate(Product, evidence).Should().BeNull();
    }

    [Fact]
    public void Evaluate_ShouldRequireReview_WhenGtinsDiffer()
    {
        MatchEvidence evidence = new("4006381333948", "Vicat", null, null);

        OfferMatchingPolicy.Evaluate(Product, evidence).Should().BeNull();
    }
}
