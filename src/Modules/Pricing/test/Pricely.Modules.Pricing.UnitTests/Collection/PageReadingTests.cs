using System.Net;
using FluentAssertions;
using Pricely.Modules.Pricing.Infrastructure.Collection;

namespace Pricely.Modules.Pricing.UnitTests.Collection;

public sealed class PageReadingTests
{
    [Theory]
    [InlineData("93.184.216.34")]
    [InlineData("8.8.8.8")]
    [InlineData("2606:4700:4700::1111")]
    public void PublicAddressGuard_ShouldAcceptAPublicAddress(string address) =>
        PublicAddressGuard.IsPublic(IPAddress.Parse(address)).Should().BeTrue();

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.5")]
    [InlineData("172.20.1.1")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fd00::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::127.0.0.1")]
    [InlineData("64:ff9b::a9fe:a9fe")]
    [InlineData("2002:a9fe:a9fe::1")]
    [InlineData("2001:db8::1")]
    [InlineData("192.0.0.170")]
    [InlineData("198.18.0.1")]
    [InlineData("203.0.113.5")]
    public void PublicAddressGuard_ShouldRefuseALocalOrPrivateAddress(string address) =>
        PublicAddressGuard.IsPublic(IPAddress.Parse(address)).Should().BeFalse();

    [Fact]
    public void HtmlText_ShouldKeepTheVisibleText_AndTheStructuredProductData()
    {
        const string html = """
            <html><head>
              <title>Ciment</title>
              <meta property="product:price:amount" content="9.40">
              <script type="application/ld+json">{"@type":"Product","gtin13":"3456789012344",
                "offers":{"price":"9.40"}}</script>
              <script>var tracking = "9999,99";</script>
              <style>.price { color: red }</style>
            </head><body>
              <h1>Ciment CEM II 35 kg</h1><!-- 1,00 € -->
              <p class="price">9,40&nbsp;&euro;</p>
            </body></html>
            """;

        string text = HtmlText.Extract(html);

        text.Should().Contain("[données structurées] {\"@type\":\"Product\",\"gtin13\":\"3456789012344\", \"offers\":{\"price\":\"9.40\"}}")
            .And.Contain("[product:price:amount] 9.40")
            .And.Contain("Ciment CEM II 35 kg")
            .And.Contain("9,40 €")
            .And.NotContain("9999,99").And.NotContain("color").And.NotContain("1,00");
    }
}
