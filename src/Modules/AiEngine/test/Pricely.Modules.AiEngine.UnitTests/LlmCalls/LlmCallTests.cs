using FluentAssertions;
using Pricely.Modules.AiEngine.Domain.LlmCalls;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.UnitTests.LlmCalls;

public sealed class LlmCallTests
{
    private static readonly DateTime CalledOn = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Record_ShouldFreezeCost_WithPriceInForceOnTheDayOfTheCall()
    {
        Model model = Model.Create(Provider.Anthropic, "claude-sonnet-5-5", "Claude Sonnet", CalledOn).Value;
        model.SetPrice(new DateOnly(2026, 1, 1), new PriceValues(3m, 0.30m, 3.75m, 15m), PriceSource.Manual, CalledOn);

        var call = LlmCall.Record(Details(new TokenUsage(1_000_000, 500_000, 0, 200_000, 0)), model);

        call.ModelId.Should().Be(model.Id);
        call.InputCost.Should().Be(3m);
        call.CachedInputCost.Should().Be(0.15m);
        call.CacheWriteCost.Should().Be(0m);
        call.OutputCost.Should().Be(3m);
        call.TotalCost.Should().Be(6.15m);
        call.TotalTokens.Should().Be(1_700_000);
    }

    [Fact]
    public void Record_ShouldStayUnpriced_WhenModelIsUnknown()
    {
        var call = LlmCall.Record(Details(new TokenUsage(10, 0, 0, 5, 0)), null);

        call.ModelId.Should().BeNull();
        call.TotalCost.Should().BeNull();
    }

    [Fact]
    public void Record_ShouldStayUnpriced_WhenModelHasNoPriceYet()
    {
        Model model = Model.Create(Provider.Anthropic, "claude-sonnet-5-5", "Claude Sonnet", CalledOn).Value;

        var call = LlmCall.Record(Details(new TokenUsage(10, 0, 0, 5, 0)), model);

        call.TotalCost.Should().BeNull();
    }

    [Fact]
    public void Record_ShouldTruncateLongErrorMessages()
    {
        LlmCallDetails details = new()
        {
            SessionId = "session-1",
            SourceModule = "Pricing",
            CalledOnUtc = CalledOn,
            FeatureCode = "pricing.test",
            ModelProvider = "Anthropic",
            ModelName = "claude-sonnet-5-5",
            TokenUsage = default,
            IsSuccess = false,
            ErrorMessage = new string('e', LlmCall.ErrorMessageMaxLength + 10)
        };

        var call = LlmCall.Record(details, null);

        call.ErrorMessage.Should().HaveLength(LlmCall.ErrorMessageMaxLength);
    }

    [Fact]
    public void Record_ShouldTruncateTooLongReportedValues()
    {
        LlmCallDetails details = Details(new TokenUsage(1, 0, 0, 1, 0));
        LlmCallDetails tooLong = new()
        {
            SessionId = new string('s', LlmCall.SessionIdMaxLength + 5),
            SourceModule = details.SourceModule,
            CalledOnUtc = details.CalledOnUtc,
            FeatureCode = details.FeatureCode,
            ModelProvider = details.ModelProvider,
            ModelName = new string('m', LlmCall.ModelNameMaxLength + 5),
            TokenUsage = details.TokenUsage,
            IsSuccess = true
        };

        var call = LlmCall.Record(tooLong, null);

        call.SessionId.Should().HaveLength(LlmCall.SessionIdMaxLength);
        call.ModelName.Should().HaveLength(LlmCall.ModelNameMaxLength);
    }

    private static LlmCallDetails Details(TokenUsage usage) =>
        new()
        {
            SessionId = "session-1",
            SourceModule = "Pricing",
            CalledOnUtc = CalledOn,
            FeatureCode = "pricing.test",
            ModelProvider = "Anthropic",
            ModelName = "claude-sonnet-5-5",
            TokenUsage = usage,
            ResponseTimeMs = 1200,
            IsSuccess = true
        };
}
