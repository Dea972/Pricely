using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.UnitTests.Merchants;

public sealed class MerchantTests
{
    private static readonly Merchant Comptoir = Merchant.Register("Comptoir", "https://www.comptoir.test").Value;

    [Theory]
    [InlineData("https://www.comptoir.test/p/ciment")]
    [InlineData("https://comptoir.test/p/ciment")]
    [InlineData("http://boutique.comptoir.test/p?id=1")]
    [InlineData("https://WWW.COMPTOIR.TEST/p")]
    [InlineData("https://www.comptoir.test:443/p")]
    public void Owns_ShouldAcceptTheSiteAndItsSubDomains(string address) =>
        Comptoir.Owns(new Uri(address)).Should().BeTrue();

    [Theory]
    [InlineData("https://comptoir.test.evil.example/p")]
    [InlineData("https://faux-comptoir.test/p")]
    [InlineData("https://evilcomptoir.test/p")]
    [InlineData("https://user:secret@www.comptoir.test/p")]
    [InlineData("ftp://www.comptoir.test/p")]
    [InlineData("file:///etc/passwd")]
    [InlineData("https://www.comptoir.test:8443/p")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    public void Owns_ShouldRefuseAnyOtherAddress(string address) =>
        Comptoir.Owns(new Uri(address)).Should().BeFalse();

    [Fact]
    public void ConfigureSearch_ShouldBuildTheSearchAddress_WithTheTextEscaped()
    {
        Merchant merchant = Merchant.Register("Comptoir", "https://www.comptoir.test", "https://www.comptoir.test/recherche?q={query}").Value;

        merchant.SearchAddress("ciment CEM II & sable")!.AbsoluteUri
            .Should().Be("https://www.comptoir.test/recherche?q=ciment%20CEM%20II%20%26%20sable");
    }

    [Theory]
    [InlineData("https://www.comptoir.test/recherche")]
    [InlineData("https://www.autre-site.test/recherche?q={query}")]
    [InlineData("javascript:alert('{query}')")]
    public void ConfigureSearch_ShouldRefuseATemplateWithoutQuery_OrOutsideTheSite(string template)
    {
        Result<Merchant> merchant = Merchant.Register("Comptoir", "https://www.comptoir.test", template);

        merchant.IsFailure.Should().BeTrue();
        merchant.Error.Code.Should().Be("Merchants.InvalidSearchTemplate");
    }

    [Fact]
    public void ADeactivatedMerchant_ShouldBeActiveAgain_OnceReactivated()
    {
        Merchant merchant = Merchant.Register("Quincaillerie", "https://www.quincaillerie.test").Value;

        merchant.Deactivate();
        merchant.IsActive.Should().BeFalse();

        merchant.Activate();
        merchant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SearchAddress_ShouldBeNull_WithoutTemplate() =>
        Comptoir.SearchAddress("ciment").Should().BeNull();

    [Theory]
    [InlineData("https://www.e.leclerc/fp/{gtin13}", "0192995410312", "https://www.e.leclerc/fp/0192995410312")]
    [InlineData("https://www.e.leclerc/fp/{gtin13}", "192995410312", "https://www.e.leclerc/fp/0192995410312")]
    [InlineData("https://www.e.leclerc/fp/{gtin13}", "00192995410312", "https://www.e.leclerc/fp/0192995410312")]
    [InlineData("https://www.site.test/p?ean={gtin14}", "3596700101088", "https://www.site.test/p?ean=03596700101088")]
    public void ProductPageAddress_ShouldWriteTheGtinTheWayTheTemplateAsks(string template, string gtin, string expected) =>
        Merchant.ProductPageAddress(template, gtin).Should().Be(new Uri(expected));

    [Theory]
    [InlineData("https://www.e.leclerc/fp/{gtin13}", "13596700101085")]
    [InlineData("https://www.e.leclerc/fp/{gtin13}", "pas-un-gtin")]
    [InlineData("https://www.e.leclerc/fp/{gtin13}", null)]
    [InlineData(null, "3596700101088")]
    public void ProductPageAddress_ShouldBeNull_WhenTheGtinCannotBeWrittenSo(string? template, string? gtin) =>
        Merchant.ProductPageAddress(template, gtin).Should().BeNull();

    [Theory]
    [InlineData("https://www.comptoir.test/fp/{gtin13}", true)]
    [InlineData("https://www.comptoir.test/p?ean={gtin14}", true)]
    [InlineData("https://www.comptoir.test/fp/{query}", false)]
    [InlineData("https://ailleurs.test/fp/{gtin13}", false)]
    public void ConfigureProductPage_ShouldAcceptOnlyAGtinPageOfTheSite(string template, bool accepted)
    {
        Merchant merchant = Merchant.Register("Comptoir", "https://www.comptoir.test").Value;

        Result result = merchant.ConfigureProductPage(template);

        result.IsSuccess.Should().Be(accepted);
        merchant.ProductPageTemplate.Should().Be(accepted ? template : null);
    }
}
