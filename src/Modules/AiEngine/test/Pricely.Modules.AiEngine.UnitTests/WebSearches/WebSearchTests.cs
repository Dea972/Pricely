using FluentAssertions;
using Pricely.Modules.AiEngine.Domain.WebSearches;

namespace Pricely.Modules.AiEngine.UnitTests.WebSearches;

public sealed class WebSearchTests
{
    private static readonly WebSearchDetails Details = new()
    {
        SessionId = "conversation-1",
        SearchedOnUtc = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc),
        Provider = "Anthropic",
        Searches = 3,
        Results = 8,
        ResponseTimeMs = 4200,
        IsSuccess = true
    };

    [Fact]
    public void Record_ShouldFreezeTheCost_AtThePricePerSearch() =>
        WebSearch.Record(Details, 0.01m).Should().BeEquivalentTo(new { PricePerSearch = 0.01m, Cost = 0.03m, Searches = 3 });

    [Fact]
    public void Record_ShouldLeaveTheCostUnknown_WithoutAPrice() =>
        WebSearch.Record(Details with { Provider = "Brave" }, null).Cost.Should().BeNull();

    [Fact]
    public void Record_ShouldCostNothing_WhenTheProviderBilledNoSearch() =>
        WebSearch.Record(Details with { Searches = 0, IsSuccess = false, ErrorMessage = "401" }, 0.01m).Cost.Should().Be(0m);
}
