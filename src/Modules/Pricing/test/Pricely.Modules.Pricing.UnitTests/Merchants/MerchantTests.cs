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
    public void SearchAddress_ShouldBeNull_WithoutTemplate() =>
        Comptoir.SearchAddress("ciment").Should().BeNull();
}
