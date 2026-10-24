using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;

namespace Pricely.Modules.Pricing.UnitTests.CollectionProfiles;

public sealed class CollectionProfileTests
{
    [Fact]
    public void Define_ShouldAssignPrioritiesFromListOrder()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        Result<CollectionProfile> result = CollectionProfile.Define(Guid.NewGuid(), [first, second], "  Compare unit prices  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.PreferredMerchants.Select(m => (m.MerchantId, m.Priority))
            .Should().Equal((first, 1), (second, 2));
        result.Value.Instructions.Should().Be("Compare unit prices");
    }

    [Fact]
    public void Define_ShouldFail_WhenAMerchantIsListedTwice()
    {
        Guid merchantId = Guid.NewGuid();

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
        Guid kept = Guid.NewGuid();
        Guid removed = Guid.NewGuid();
        Guid added = Guid.NewGuid();
        CollectionProfile profile = CollectionProfile.Define(Guid.NewGuid(), [kept, removed], null).Value;

        Result result = profile.Update([added, kept], "   ");

        result.IsSuccess.Should().BeTrue();
        profile.PreferredMerchants.Select(m => (m.MerchantId, m.Priority))
            .Should().Equal((added, 1), (kept, 2));
        profile.Instructions.Should().BeNull();
    }
}
