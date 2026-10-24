using FluentAssertions;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;

namespace Pricely.Modules.Pricing.UnitTests.CollectionProfiles;

public sealed class CollectionProfileResolverTests
{
    private static readonly Guid Root = Guid.NewGuid();
    private static readonly Guid Parent = Guid.NewGuid();
    private static readonly Guid Leaf = Guid.NewGuid();
    private static readonly Guid[] Lineage = [Leaf, Parent, Root];

    [Fact]
    public void Resolve_ShouldUseOwnPreferredMerchants_WhenDefined()
    {
        Guid leafMerchant = Guid.NewGuid();
        CollectionProfile[] profiles =
        [
            Profile(Root, [Guid.NewGuid()], null),
            Profile(Leaf, [leafMerchant], null)
        ];

        ResolvedCollectionProfile resolved = CollectionProfileResolver.Resolve(Lineage, profiles);

        resolved.PreferredMerchantIds.Should().Equal(leafMerchant);
        resolved.PreferredMerchantsSourceCategoryId.Should().Be(Leaf);
    }

    [Fact]
    public void Resolve_ShouldInheritPreferredMerchantsFromNearestAncestor()
    {
        Guid parentMerchant = Guid.NewGuid();
        CollectionProfile[] profiles =
        [
            Profile(Root, [Guid.NewGuid()], null),
            Profile(Parent, [parentMerchant], null),
            Profile(Leaf, [], "Leaf only refines instructions")
        ];

        ResolvedCollectionProfile resolved = CollectionProfileResolver.Resolve(Lineage, profiles);

        resolved.PreferredMerchantIds.Should().Equal(parentMerchant);
        resolved.PreferredMerchantsSourceCategoryId.Should().Be(Parent);
    }

    [Fact]
    public void Resolve_ShouldConcatenateInstructionsFromRootToLeaf()
    {
        CollectionProfile[] profiles =
        [
            Profile(Leaf, [], "Distinguish CEM I and CEM II."),
            Profile(Root, [], "Compare unit prices including VAT."),
            Profile(Parent, [], "Bags of 25 or 35 kg.")
        ];

        ResolvedCollectionProfile resolved = CollectionProfileResolver.Resolve(Lineage, profiles);

        resolved.Instructions.Should().Be(
            "Compare unit prices including VAT." +
            CollectionProfileResolver.InstructionsSeparator +
            "Bags of 25 or 35 kg." +
            CollectionProfileResolver.InstructionsSeparator +
            "Distinguish CEM I and CEM II.");
    }

    [Fact]
    public void Resolve_ShouldReturnEmptyProfile_WhenNoProfileIsDefined()
    {
        ResolvedCollectionProfile resolved = CollectionProfileResolver.Resolve(Lineage, []);

        resolved.HasPreferredMerchants.Should().BeFalse();
        resolved.PreferredMerchantsSourceCategoryId.Should().BeNull();
        resolved.Instructions.Should().BeEmpty();
    }

    private static CollectionProfile Profile(Guid categoryId, Guid[] merchantIds, string? instructions) =>
        CollectionProfile.Define(categoryId, merchantIds, instructions).Value;
}
