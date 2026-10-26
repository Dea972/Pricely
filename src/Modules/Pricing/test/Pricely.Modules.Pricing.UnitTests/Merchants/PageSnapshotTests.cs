using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.UnitTests.Merchants;

public sealed class PageSnapshotTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("Prix : 9,40 € TTC", 9.40)]
    [InlineData("Prix : 9,4 €", 9.40)]
    [InlineData("$9.40", 9.40)]
    [InlineData("1\u00A0234,50 €", 1234.50)]
    [InlineData("1\u202F234,50 €", 1234.50)]
    [InlineData("1,234.50", 1234.50)]
    [InlineData("1.234,50 €", 1234.50)]
    [InlineData("12 €", 12)]
    [InlineData("{\"price\":\"9.40\"}", 9.40)]
    public void ShowsPrice_ShouldFindTheAmount_WrittenAnyCommonWay(string content, decimal amount) =>
        Snapshot(content).ShowsPrice(amount).Should().BeTrue();

    [Theory]
    [InlineData("Prix : 9,40 €", 9.04)]
    [InlineData("Prix : 94,00 €", 9.40)]
    [InlineData("Lot de 10 : 94,00 €", 940)]
    public void ShowsPrice_ShouldNotFindAnotherAmount(string content, decimal amount) =>
        Snapshot(content).ShowsPrice(amount).Should().BeFalse();

    [Fact]
    public void Capture_ShouldRefuseAPageOutsideTheMerchantSite()
    {
        Merchant merchant = Merchant.Register("Comptoir", "https://www.comptoir.test").Value;

        Result<PageSnapshot> snapshot = PageSnapshot.Capture(merchant, new Uri("https://autre.test/p"), "9,40 €", Now);

        snapshot.Error.Code.Should().Be("Merchants.AddressOutsideMerchant");
    }

    [Fact]
    public void Capture_ShouldRefuseAnInactiveMerchant()
    {
        Merchant merchant = Merchant.Register("Comptoir", "https://www.comptoir.test").Value;
        merchant.Deactivate();

        PageSnapshot.Capture(merchant, new Uri("https://www.comptoir.test/p"), "9,40 €", Now).IsFailure.Should().BeTrue();
    }

    private static PageSnapshot Snapshot(string content) =>
        PageSnapshot.Capture(Merchant.Register("Comptoir", "https://www.comptoir.test").Value, new Uri("https://www.comptoir.test/p"), content, Now).Value;
}
