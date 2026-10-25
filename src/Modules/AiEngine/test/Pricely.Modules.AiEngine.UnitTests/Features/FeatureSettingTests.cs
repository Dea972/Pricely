using FluentAssertions;
using Pricely.Modules.AiEngine.Domain.Features;

namespace Pricely.Modules.AiEngine.UnitTests.Features;

public sealed class FeatureSettingTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Effective_ShouldPreferOwnSetting()
    {
        var own = FeatureSetting.Create("pricing.test", Guid.NewGuid(), Now);
        var fallback = FeatureSetting.Create(FeatureSetting.DefaultCode, Guid.NewGuid(), Now);

        FeatureSetting.Effective(own, fallback).Should().BeSameAs(own);
    }

    [Fact]
    public void Effective_ShouldFallBackToDefaultSetting()
    {
        var fallback = FeatureSetting.Create(FeatureSetting.DefaultCode, Guid.NewGuid(), Now);

        FeatureSetting.Effective(null, fallback).Should().BeSameAs(fallback);
        fallback.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Target_ShouldChangeModel()
    {
        var setting = FeatureSetting.Create("pricing.test", Guid.NewGuid(), Now);
        var modelId = Guid.NewGuid();

        setting.Target(modelId, Now.AddDays(1));

        setting.ModelId.Should().Be(modelId);
        setting.UpdatedOnUtc.Should().Be(Now.AddDays(1));
    }
}
