using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Domain.Products;
using Pricely.Modules.Catalog.UnitTests.Abstractions;

namespace Pricely.Modules.Catalog.UnitTests.Products;

public sealed class ProductTests : BaseTest
{
    private static readonly Gtin SampleGtin = Gtin.Create("4006381333931").Value;

    [Fact]
    public void Register_ShouldCreateProduct_AndRaiseDomainEvent()
    {
        var categoryId = Guid.NewGuid();

        Result<Product> result = Product.Register(
            new ProductDescription(" Ciment CEM II 35 kg ", "Vicat", SampleGtin, "CEM2-35"),
            categoryId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Ciment CEM II 35 kg");
        result.Value.ProductCategoryId.Should().Be(categoryId);
        AssertDomainEventWasPublished<ProductRegisteredDomainEvent>(result.Value)
            .ProductId.Should().Be(result.Value.Id);
    }

    [Fact]
    public void Register_ShouldStoreBlankManufacturerReferenceAsNull()
    {
        Result<Product> result = Product.Register(
            new ProductDescription("Laine de verre 100 mm", "Isover", null, "  "),
            Guid.NewGuid());

        result.Value.ManufacturerReference.Should().BeNull();
    }

    [Fact]
    public void Register_ShouldFail_WhenBrandIsMissing()
    {
        Result<Product> result = Product.Register(
            new ProductDescription("Laine de verre 100 mm", " ", null, null),
            Guid.NewGuid());

        result.Error.Should().Be(ProductErrors.InvalidBrand);
    }

    [Fact]
    public void Update_ShouldChangeCategory_AndRaiseDomainEvent()
    {
        Product product = Product.Register(
            new ProductDescription("Laine de verre 100 mm", "Isover", null, null),
            Guid.NewGuid()).Value;
        product.ClearDomainEvents();
        var newCategoryId = Guid.NewGuid();

        Result result = product.Update(
            new ProductDescription("Laine de verre 120 mm", "Isover", SampleGtin, null),
            newCategoryId);

        result.IsSuccess.Should().BeTrue();
        product.ProductCategoryId.Should().Be(newCategoryId);
        product.Gtin.Should().Be(SampleGtin);
        AssertDomainEventWasPublished<ProductUpdatedDomainEvent>(product);
    }
}
