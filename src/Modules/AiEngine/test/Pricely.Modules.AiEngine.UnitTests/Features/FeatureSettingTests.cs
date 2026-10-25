using FluentAssertions;
using Pricely.Modules.AiEngine.Domain.Features;

namespace Pricely.Modules.AiEngine.UnitTests.Features;

public sealed class FeatureSettingTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Effective_ShouldPreferOwnSetting()
    {
        FeatureSetting own = FeatureSetting.Create("pricing.test", Guid.NewGuid(), Now).Value;
        FeatureSetting fallback = FeatureSetting.Create(FeatureSetting.DefaultCode, Guid.NewGuid(), Now).Value;

        FeatureSetting.Effective(own, fallback).Should().BeSameAs(own);
    }

    [Fact]
    public void Effective_ShouldFallBackToDefaultSetting()
    {
        FeatureSetting fallback = FeatureSetting.Create(FeatureSetting.DefaultCode, Guid.NewGuid(), Now).Value;

        FeatureSetting.Effective(null, fallback).Should().BeSameAs(fallback);
        fallback.IsDefault.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_ShouldFail_WhenFeatureCodeIsBlank(string featureCode)
    {
        FeatureSetting.Create(featureCode, Guid.NewGuid(), Now).Error.Should().Be(FeatureSettingErrors.InvalidFeatureCode);
    }

    [Fact]
    public void Target_ShouldChangeModel()
    {
        FeatureSetting setting = FeatureSetting.Create("pricing.test", Guid.NewGuid(), Now).Value;
        var modelId = Guid.NewGuid();

        setting.Target(modelId, Now.AddDays(1));

        setting.ModelId.Should().Be(modelId);
        setting.UpdatedOnUtc.Should().Be(Now.AddDays(1));
    }
}
