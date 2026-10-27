using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Abstractions.Data;
using Pricely.Modules.Catalog.Application.Products.FindOrRegisterProduct;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.UnitTests.Products;

public sealed class FindOrRegisterProductTests
{
    private readonly InMemoryCategories _categories = new();
    private readonly InMemoryProducts _products = new();
    private readonly CountingUnitOfWork _unitOfWork = new();

    [Fact]
    public void Gtin_ShouldListTheWaysToWriteIt()
    {
        Gtin.Create("3760000100014").Value.EquivalentForms().Should().BeEquivalentTo("3760000100014", "03760000100014");
        Gtin.Create("96385074").Value.EquivalentForms().Should().BeEquivalentTo("96385074", "000096385074", "0000096385074", "00000096385074");
    }

    [Fact]
    public async Task Find_ShouldReturnTheProductOfTheSameGtin_WrittenWithAnotherLength()
    {
        ProductCategory cement = Root("Ciment");
        Product known = Register("Ciment 35 kg", "Cimenterie", "3760000100014", null, cement);

        Result<FoundProductResponse> result = await Handler().Handle(
            new FindOrRegisterProductCommand("Ciment gris 35kg", "Cimenterie", "03760000100014", null, ["Autre"]),
            CancellationToken.None);

        result.Value.Should().BeEquivalentTo(new { ProductId = known.Id, IsNew = false, ProductCategoryId = cement.Id });
        result.Value.CreatedCategories.Should().BeEmpty();
        _unitOfWork.Saves.Should().Be(0);
    }

    [Fact]
    public async Task Find_ShouldReturnTheProductOfTheSameBrandAndReference_WrittenDifferently()
    {
        Product known = Register("Mortier 25 kg", "Weber", null, "MR-25", Root("Mortier"));

        Result<FoundProductResponse> result = await Handler().Handle(
            new FindOrRegisterProductCommand("Mortier", "weber", null, "mr 25", ["Mortier"]),
            CancellationToken.None);

        result.Value.ProductId.Should().Be(known.Id);
        result.Value.IsNew.Should().BeFalse();
    }

    [Fact]
    public async Task Register_ShouldCreateOnlyTheMissingLevelsOfThePath_InOneSave()
    {
        ProductCategory structure = Root("Gros œuvre");

        Result<FoundProductResponse> result = await Handler().Handle(
            new FindOrRegisterProductCommand("Ciment prompt 5 kg", "Vicat", "3760000100014", null, ["gros ŒUVRE", "Ciment", "Ciment prompt"]),
            CancellationToken.None);

        result.Value.IsNew.Should().BeTrue();
        result.Value.CreatedCategories.Should().Equal("Ciment", "Ciment prompt");
        ProductCategory leaf = _categories.Items.Single(c => c.Name == "Ciment prompt");
        _categories.Items.Single(c => c.Name == "Ciment").ParentId.Should().Be(structure.Id);
        leaf.Depth.Should().Be(3);
        _products.Items.Should().ContainSingle(p => p.Id == result.Value.ProductId && p.ProductCategoryId == leaf.Id);
        _unitOfWork.Saves.Should().Be(1);
    }

    [Fact]
    public async Task Register_ShouldRefuseAPathDeeperThanTheTree()
    {
        Result<FoundProductResponse> result = await Handler().Handle(
            new FindOrRegisterProductCommand("Vis", "Spit", null, null, ["A", "B", "C", "D"]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _products.Items.Should().BeEmpty();
        _unitOfWork.Saves.Should().Be(0);
    }

    private FindOrRegisterProductCommandHandler Handler() => new(_products, _categories, _unitOfWork);

    private ProductCategory Root(string name)
    {
        ProductCategory category = ProductCategory.CreateRoot(name).Value;
        _categories.Insert(category);

        return category;
    }

    private Product Register(string name, string brand, string? gtin, string? reference, ProductCategory category)
    {
        ProductDescription description = new(name, brand, gtin is null ? null : Gtin.Create(gtin).Value, reference);
        Product product = Product.Register(description, category.Id).Value;
        _products.Insert(product);

        return product;
    }

    private sealed class InMemoryCategories : IProductCategoryRepository
    {
        public List<ProductCategory> Items { get; } = [];

        public Task<ProductCategory?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Find(c => c.Id == id));

        public Task<IReadOnlyCollection<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ProductCategory>>([.. Items]);

        public void Insert(ProductCategory category) => Items.Add(category);
    }

    private sealed class InMemoryProducts : IProductRepository
    {
        public List<Product> Items { get; } = [];

        public Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Find(p => p.Id == id));

        public Task<bool> ExistsWithGtinAsync(Gtin gtin, Guid? excludedProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Exists(p => p.Gtin == gtin && p.Id != excludedProductId));

        public Task<Product?> FindByGtinAsync(Gtin gtin, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Find(p => p.Gtin == gtin));

        public Task<Product?> FindByEquivalentGtinAsync(Gtin gtin, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Find(p => p.Gtin is not null && gtin.EquivalentForms().Contains(p.Gtin.Value)));

        public Task<IReadOnlyList<Product>> GetByBrandAsync(string brand, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>([.. Items.Where(p => string.Equals(p.Brand, brand.Trim(), StringComparison.OrdinalIgnoreCase))]);

        public void Insert(Product product) => Items.Add(product);
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int Saves { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Saves++;

            return Task.FromResult(0);
        }
    }
}
