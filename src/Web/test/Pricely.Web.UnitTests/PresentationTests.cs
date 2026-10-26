using FluentAssertions;
using Pricely.Web.Api;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.UnitTests;

public sealed class PresentationTests
{
    [Theory]
    [InlineData(0.046, "+4,6 %")]
    [InlineData(-0.046, "−4,6 %")]
    [InlineData(0, "0,0 %")]
    [InlineData(8.54, "+854,0 %")]
    public void Deviation_ShouldBeWrittenInFrench_WithItsSign(double fraction, string expected) =>
        Format.Deviation((decimal)fraction).Should().Be(expected);

    [Fact]
    public void Price_ShouldBeWrittenInFrench()
    {
        Format.Price(9.4m, "EUR").Should().Be("9,40 €");
        Format.Price(1234.5m, "USD").Should().Be("1 234,50 USD");
        Format.Price(null, "EUR").Should().Be("—");
    }

    [Theory]
    [InlineData("https://www.enseigne.test/ciment", "https://www.enseigne.test/ciment")]
    [InlineData("http://enseigne.test/p", "http://enseigne.test/p")]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("data:text/html,<b>x</b>", null)]
    [InlineData("/relative", null)]
    [InlineData(null, null)]
    public void MerchantLinks_ShouldOnlyBeHttpAddresses(string? address, string? expected) =>
        Format.ExternalHref(address).Should().Be(expected);

    [Fact]
    public void Ago_ShouldBeRelative_ThenADate()
    {
        var now = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

        Format.Ago(now.UtcDateTime.AddHours(-2), now).Should().Be("il y a 2 h");
        Format.Ago(now.UtcDateTime.AddHours(-30), now).Should().Be("hier");
        Format.Ago(new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc), now).Should().Be("1 oct. 2026");
    }

    [Fact]
    public void CategoryPath_ShouldGoFromTheRootToTheCategory()
    {
        var root = Guid.NewGuid();
        var leaf = Guid.NewGuid();
        ProductCategoryDto[] categories = [new(leaf, "Ciment", root, 1), new(root, "Gros œuvre", null, 0)];

        CategoryPath.Build(categories, leaf).Should().Be("Gros œuvre › Ciment");
        CategoryPath.Build(categories, Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void Chart_ShouldDrawALinePerMerchant_WithoutTheFlaggedPrices()
    {
        var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        PriceHistoryEntryDto[] entries =
        [
            Entry("Comptoir", 9.90m, start),
            Entry("Comptoir", 9.40m, start.AddDays(80)),
            Entry("Négoce", 9.85m, start.AddDays(10)),
            Entry("BricoPlus", 94m, start.AddDays(70), flagged: true)
        ];

        var chart = PriceChartModel.Build(entries);

        chart.Series.Select(s => s.Name).Should().Equal("Comptoir", "Négoce");
        chart.FlaggedCount.Should().Be(1);
        chart.Series[0].Points.Split(' ').Should().HaveCount(2);
        chart.Series.Select(s => s.Style).Should().OnlyHaveUniqueItems("each line has its own dash, not only its colour");
        chart.Description.Should().Contain("Comptoir de 9,90 € à 9,40 €");
    }

    [Fact]
    public void Chart_WithoutPrices_ShouldSaySo() =>
        PriceChartModel.Build([]).Description.Should().Be("Aucun relevé à tracer.");

    private static PriceHistoryEntryDto Entry(string merchant, decimal amount, DateTime at, bool flagged = false) =>
        new()
        {
            OfferId = Guid.NewGuid(),
            MerchantId = DeterministicId(merchant),
            MerchantName = merchant,
            Amount = amount,
            Currency = "EUR",
            Availability = ApiValues.InStock,
            ObservedOnUtc = at,
            IsFlaggedAsAnomaly = flagged
        };

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Guid> MerchantIds = new(StringComparer.Ordinal);

    private static Guid DeterministicId(string name) => MerchantIds.GetOrAdd(name, _ => Guid.NewGuid());
}
