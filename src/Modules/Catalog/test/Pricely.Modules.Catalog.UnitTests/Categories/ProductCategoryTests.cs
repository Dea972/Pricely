using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.UnitTests.Abstractions;

namespace Pricely.Modules.Catalog.UnitTests.Categories;

public sealed class ProductCategoryTests : BaseTest
{
    [Fact]
    public void CreateRoot_ShouldCreateCategoryAtDepthOne()
    {
        Result<ProductCategory> result = ProductCategory.CreateRoot("Gros œuvre");

        result.IsSuccess.Should().BeTrue();
        result.Value.Depth.Should().Be(1);
        result.Value.ParentId.Should().BeNull();
    }

    [Fact]
    public void CreateRoot_ShouldRaiseDomainEvent()
    {
        ProductCategory category = ProductCategory.CreateRoot("Gros œuvre").Value;

        ProductCategoryCreatedDomainEvent domainEvent =
            AssertDomainEventWasPublished<ProductCategoryCreatedDomainEvent>(category);

        domainEvent.ProductCategoryId.Should().Be(category.Id);
    }

    [Fact]
    public void CreateChild_ShouldIncrementDepthAndReferenceParent()
    {
        ProductCategory parent = ProductCategory.CreateRoot("Gros œuvre").Value;

        Result<ProductCategory> result = ProductCategory.CreateChild("Ciment", parent);

        result.IsSuccess.Should().BeTrue();
        result.Value.Depth.Should().Be(2);
        result.Value.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public void CreateChild_ShouldFail_WhenParentIsAtMaximumDepth()
    {
        ProductCategory level1 = ProductCategory.CreateRoot("Gros œuvre").Value;
        ProductCategory level2 = ProductCategory.CreateChild("Ciment", level1).Value;
        ProductCategory level3 = ProductCategory.CreateChild("Ciment prompt", level2).Value;

        Result<ProductCategory> result = ProductCategory.CreateChild("Trop profond", level3);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProductCategories.MaxDepthExceeded");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRoot_ShouldFail_WhenNameIsBlank(string name)
    {
        Result<ProductCategory> result = ProductCategory.CreateRoot(name);

        result.Error.Should().Be(ProductCategoryErrors.InvalidName);
    }

    [Fact]
    public void Rename_ShouldRaiseDomainEvent_WhenNameChanges()
    {
        ProductCategory category = ProductCategory.CreateRoot("Isolation").Value;
        category.ClearDomainEvents();

        Result result = category.Rename("Isolation thermique");

        result.IsSuccess.Should().BeTrue();
        AssertDomainEventWasPublished<ProductCategoryRenamedDomainEvent>(category)
            .Name.Should().Be("Isolation thermique");
    }

    [Fact]
    public void Rename_ShouldNotRaiseDomainEvent_WhenNameIsUnchanged()
    {
        ProductCategory category = ProductCategory.CreateRoot("Isolation").Value;
        category.ClearDomainEvents();

        category.Rename("Isolation");

        category.DomainEvents.Should().BeEmpty();
    }
}
