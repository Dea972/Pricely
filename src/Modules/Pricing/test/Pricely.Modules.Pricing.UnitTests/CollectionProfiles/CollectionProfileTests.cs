using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;

namespace Pricely.Modules.Pricing.UnitTests.CollectionProfiles;

public sealed class CollectionProfileTests
{
    [Fact]
    public void Define_ShouldAssignPrioritiesFromListOrder()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        Result<CollectionProfile> result = CollectionProfile.Define(Guid.NewGuid(), [first, second], "  Compare unit prices  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.PreferredMerchants.Select(m => (m.MerchantId, m.Priority))
            .Should().Equal((first, 1), (second, 2));
        result.Value.Instructions.Should().Be("Compare unit prices");
    }

    [Fact]
    public void Define_ShouldFail_WhenAMerchantIsListedTwice()
    {
        var merchantId = Guid.NewGuid();

        Result<CollectionProfile> result = CollectionProfile.Define(Guid.NewGuid(), [merchantId, merchantId], null);

        result.Error.Should().Be(CollectionProfileErrors.DuplicatePreferredMerchant);
    }

    [Fact]
    public void Define_ShouldFail_WhenInstructionsAreTooLong()
    {
        string instructions = new('x', CollectionProfile.InstructionsMaxLength + 1);

        Result<CollectionProfile> result = CollectionProfile.Define(Guid.NewGuid(), [], instructions);

        result.Error.Should().Be(CollectionProfileErrors.InstructionsTooLong);
    }

    [Fact]
    public void Update_ShouldReorderKeepAndRemoveMerchants()
    {
        var kept = Guid.NewGuid();
        var removed = Guid.NewGuid();
        var added = Guid.NewGuid();
        CollectionProfile profile = CollectionProfile.Define(Guid.NewGuid(), [kept, removed], null).Value;

        Result result = profile.Update([added, kept], "   ");

        result.IsSuccess.Should().BeTrue();
        profile.PreferredMerchants.Select(m => (m.MerchantId, m.Priority))
            .Should().Equal((added, 1), (kept, 2));
        profile.Instructions.Should().BeNull();
    }
}
