using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.UnitTests.Products;

public sealed class GtinTests
{
    [Theory]
    [InlineData("96385074")]
    [InlineData("036000291452")]
    [InlineData("4006381333931")]
    [InlineData("10012345678902")]
    public void Create_ShouldSucceed_WhenCheckDigitIsValid(string value)
    {
        Result<Gtin> result = Gtin.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("4006381333932")]
    [InlineData("400638133393")]
    [InlineData("40063813339A1")]
    [InlineData("")]
    public void Create_ShouldFail_WhenValueIsNotAValidGtin(string value)
    {
        Result<Gtin> result = Gtin.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Products.InvalidGtin");
    }

    [Fact]
    public void Create_ShouldTrimSurroundingWhitespace()
    {
        Result<Gtin> result = Gtin.Create(" 4006381333931 ");

        result.Value.Value.Should().Be("4006381333931");
    }
}
