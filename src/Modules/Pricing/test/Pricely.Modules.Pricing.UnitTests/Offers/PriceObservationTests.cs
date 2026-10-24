using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.UnitTests.Abstractions;

namespace Pricely.Modules.Pricing.UnitTests.Offers;

public sealed class PriceObservationTests : BaseTest
{
    private static readonly DateTime Now = new(2026, 10, 24, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Record_ShouldNotFlag_WhenPriceIsCloseToMedian()
    {
        Result<PriceObservation> result = PriceObservation.Record(
            ConfirmedOffer(),
            Euros(10.50m),
            Availability.InStock,
            Now,
            [Euros(9.90m), Euros(10.20m), Euros(11m)]);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFlaggedAsAnomaly.Should().BeFalse();
        result.Value.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Record_ShouldFlagAndRaiseDomainEvent_WhenPriceDeviatesStronglyFromMedian()
    {
        Result<PriceObservation> result = PriceObservation.Record(
            ConfirmedOffer(),
            Euros(99m),
            Availability.InStock,
            Now,
            [Euros(9.90m), Euros(10.20m), Euros(11m)]);

        result.Value.IsFlaggedAsAnomaly.Should().BeTrue();
        AssertDomainEventWasPublished<PriceAnomalyDetectedDomainEvent>(result.Value);
    }

    [Fact]
    public void Record_ShouldNotFlag_WhenNoComparablePriceExists()
    {
        Result<PriceObservation> result = PriceObservation.Record(
            ConfirmedOffer(),
            Euros(99m),
            Availability.Unknown,
            Now,
            [Money.Create(10m, "USD").Value]);

        result.Value.IsFlaggedAsAnomaly.Should().BeFalse();
    }

    [Fact]
    public void Record_ShouldFail_WhenOfferIsRejected()
    {
        var offer = Offer.Propose(
            new OfferCandidate(Guid.NewGuid(), Guid.NewGuid(), new Uri("https://merchant.example/p"), null, new MatchEvidence(null, null, null, null)),
            new ProductIdentity(null, "Brand", null),
            Now);
        offer.Reject(Now);

        Result<PriceObservation> result = PriceObservation.Record(offer, Euros(10m), Availability.InStock, Now, []);

        result.Error.Code.Should().Be("Offers.Rejected");
    }

    [Theory]
    [InlineData(0, "EUR", "Offers.InvalidPrice")]
    [InlineData(-1, "EUR", "Offers.InvalidPrice")]
    [InlineData(10, "EURO", "Offers.InvalidCurrency")]
    [InlineData(10, "E1R", "Offers.InvalidCurrency")]
    public void MoneyCreate_ShouldFail_WhenInvalid(int amount, string currency, string expectedErrorCode)
    {
        Result<Money> result = Money.Create(amount, currency);

        result.Error.Code.Should().Be(expectedErrorCode);
    }

    [Fact]
    public void MoneyCreate_ShouldNormalizeCurrency()
    {
        Money.Create(12.5m, " eur ").Value.Currency.Should().Be("EUR");
    }

    private static Money Euros(decimal amount) => Money.Create(amount, "EUR").Value;

    private static Offer ConfirmedOffer() =>
        Offer.Propose(
            new OfferCandidate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Uri("https://merchant.example/p"),
                null,
                new MatchEvidence("4006381333931", null, null, null)),
            new ProductIdentity("4006381333931", "Brand", null),
            Now);
}
