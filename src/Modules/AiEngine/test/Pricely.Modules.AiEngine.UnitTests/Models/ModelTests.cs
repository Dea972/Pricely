using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.UnitTests.Models;

public sealed class ModelTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly PriceValues SonnetPrice = new(3m, 0.30m, 3.75m, 15m);

    [Fact]
    public void Create_ShouldTrimIdentifiers()
    {
        Result<Model> result = Model.Create(Provider.Anthropic, " claude-sonnet-5-5 ", " Claude Sonnet ", Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderModelId.Should().Be("claude-sonnet-5-5");
        result.Value.Label.Should().Be("Claude Sonnet");
    }

    [Fact]
    public void Create_ShouldFail_WhenProviderModelIdIsBlank()
    {
        Result<Model> result = Model.Create(Provider.Anthropic, " ", "Claude", Now);

        result.Error.Should().Be(ModelErrors.InvalidProviderModelId);
    }

    [Fact]
    public void PriceAt_ShouldReturnPriceInForceOnTheDayOfTheCall()
    {
        Model model = Model.Create(Provider.Anthropic, "claude-sonnet-5-5", "Claude Sonnet", Now).Value;
        model.SetPrice(new DateOnly(2026, 1, 1), SonnetPrice, PriceSource.Manual, Now);
        model.SetPrice(new DateOnly(2026, 6, 1), SonnetPrice with { OutputPerMillion = 12m }, PriceSource.Manual, Now);

        model.PriceAt(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc))!.OutputPerMillion.Should().Be(15m);
        model.PriceAt(new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc))!.OutputPerMillion.Should().Be(12m);
        model.PriceAt(new DateTime(2025, 12, 31, 23, 0, 0, DateTimeKind.Utc)).Should().BeNull();
    }

    [Fact]
    public void SetPrice_ShouldReplacePriceOfTheSameDay()
    {
        Model model = Model.Create(Provider.Anthropic, "claude-sonnet-5-5", "Claude Sonnet", Now).Value;
        var day = new DateOnly(2026, 1, 1);

        model.SetPrice(day, SonnetPrice, PriceSource.Manual, Now);
        model.SetPrice(day, SonnetPrice with { InputPerMillion = 2m }, PriceSource.Manual, Now);

        model.Prices.Should().ContainSingle().Which.InputPerMillion.Should().Be(2m);
    }

    [Fact]
    public void SetPrice_ShouldFail_WhenAPriceIsNegative()
    {
        Model model = Model.Create(Provider.Anthropic, "claude-sonnet-5-5", "Claude Sonnet", Now).Value;

        Result result = model.SetPrice(new DateOnly(2026, 1, 1), SonnetPrice with { OutputPerMillion = -1m }, PriceSource.Manual, Now);

        result.Error.Should().Be(ModelErrors.NegativePrice);
    }

    [Theory]
    [InlineData("Anthropic", true)]
    [InlineData("anthropic", true)]
    [InlineData("Anthropic/ManagedAgents", true)]
    [InlineData("OpenAi", false)]
    [InlineData("0", false)]
    [InlineData("", false)]
    public void FromReportedName_ShouldRecognizeKnownProviders(string reportedName, bool isKnown)
    {
        Providers.FromReportedName(reportedName).HasValue.Should().Be(isKnown);
    }
}
