using FluentAssertions;
using Pricely.Common.Application.Web;
using Pricely.Common.Infrastructure.Web;

namespace Pricely.Common.UnitTests.Web;

/// <summary>The products and prices a page declares in JSON-LD, the way the merchants of the test write them.</summary>
public sealed class StructuredDataTests
{
    [Fact]
    public void Products_ShouldReadTheOfferOfAProductPage()
    {
        const string html = """
            <script type="application/ld+json">{"@type":"Product","name":"Ciment Le Classic 32,5R","sku":"199281-1",
             "brand":{"@type":"Brand","name":"Lafarge ciment"},"gtin13":"3596700101088",
             "offers":{"@type":"Offer","availability":"InStock","price":"17.64","priceCurrency":"EUR"}}</script>
            """;

        StructuredProduct product = HtmlText.Products(html).Should().ContainSingle().Subject;

        product.Should().BeEquivalentTo(new { Name = "Ciment Le Classic 32,5R", Gtin = "3596700101088", Brand = "Lafarge ciment", Sku = "199281-1" });
        product.Offers.Should().Equal(new StructuredOffer(17.64m, "EUR", "InStock", Seller: null));
    }

    [Fact]
    public void Products_ShouldKeepTheMarketplaceSeller_AndTheBoundsOfSeveralSellers()
    {
        const string html = """
            <script type="application/ld+json">{"@type":"Product","name":"Air Max 90","gtin13":"0193154416381","offers":[
             {"@type":"Offer","priceCurrency":"EUR","price":162.95,"availability":"https://schema.org/InStock","seller":{"@type":"Organization","name":"MK DISTRIBUTION"}},
             {"@type":"AggregateOffer","offerCount":2,"lowPrice":162.95,"highPrice":188.17,"priceCurrency":"EUR"}]}</script>
            """;

        HtmlText.Products(html).Should().ContainSingle().Which.Offers.Should().Equal(
            new StructuredOffer(162.95m, "EUR", "InStock", "MK DISTRIBUTION"),
            new StructuredOffer(188.17m, "EUR", null, null, IsAggregate: true));
    }

    [Fact]
    public void Products_ShouldIgnoreAnAggregateOfNoOffer_AndAZeroPrice()
    {
        const string html = """
            <script type="application/ld+json">{"@type":"Product","name":"Stick SPF50+","gtin13":"0000030089372","offers":[
             {"@type":"Offer","url":"fp/stick"},{"@type":"AggregateOffer","offerCount":0,"lowPrice":0,"highPrice":0,"priceCurrency":"EUR"}]}</script>
            """;

        HtmlText.Products(html).Should().ContainSingle().Which.Offers.Should().BeEmpty("an unavailable product has no price");
    }

    [Fact]
    public void Products_ShouldFindAProductInAGraph_AndSkipABrokenBlock()
    {
        const string html = """
            <script type="application/ld+json">{"@type":"Product", "name": broken</script>
            <script type="application/ld+json">{"@context":"https://schema.org","@graph":[{"@type":"WebPage"},
             {"@type":["Product","Thing"],"name":"Perceuse","gtin":"3456789012344","offers":{"@type":"Offer","price":"64,10","priceCurrency":"EUR"}}]}</script>
            """;

        StructuredProduct product = HtmlText.Products(html).Should().ContainSingle().Subject;

        product.Gtin.Should().Be("3456789012344");
        product.Offers.Should().ContainSingle().Which.Price.Should().Be(64.10m);
    }
}
