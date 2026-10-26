using FluentAssertions;
using Npgsql;
using Pricely.Common.Domain;
using Pricely.Common.Infrastructure.Data;

namespace Pricely.Common.UnitTests.Data;

public sealed class UtcDateTimeTests
{
    [Fact]
    public void AsUtc_ShouldKeepAUtcInstant()
    {
        var value = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        value.AsUtc().Should().Be(value);
        value.AsUtc().Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void AsUtc_ShouldTakeAnUnspecifiedTimeAsUtc()
    {
        var value = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Unspecified);

        DateTime result = value.AsUtc();

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Hour.Should().Be(10);
    }

    [Fact]
    public void AsUtc_ShouldConvertALocalTime()
    {
        var value = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Local);

        DateTime result = value.AsUtc();

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(value.ToUniversalTime());
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]
    public void TypeHandler_ShouldSendEveryDateTimeParameterAsUtc(DateTimeKind kind)
    {
        var parameter = new NpgsqlParameter();

        new UtcDateTimeTypeHandler().SetValue(parameter, new DateTime(2026, 1, 1, 10, 0, 0, kind));

        ((DateTime)parameter.Value!).Kind.Should().Be(DateTimeKind.Utc);
    }
}
