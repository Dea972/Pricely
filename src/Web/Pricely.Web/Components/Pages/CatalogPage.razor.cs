using Microsoft.AspNetCore.Components;
using Pricely.Web.Api;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.Components.Pages;

/// <summary>
/// The typologies and the reference products (module Catalog): create and rename a typology, list, add and change the
/// products of one. The rules (three levels, GTIN check digit, unique names) are the ones of the API, shown as its errors.
/// </summary>
public partial class CatalogPage
{
    private IReadOnlyList<ProductCategoryDto> _categories = [];
    private IReadOnlyList<(ProductCategoryDto Category, int Depth)>? _tree;
    private ProductCategoryDto? _category;
    private string? _categoryPath;
    private IReadOnlyList<ProductDto>? _products;
    private Guid? _loadedCategoryId;

    private string _newCategoryName = string.Empty;
    private string _newCategoryParent = string.Empty;
    private string _rename = string.Empty;
    private ProductForm _product = new();
    private Guid? _editedProductId;
    private bool _saving;
    private string? _message;

    [Parameter]
    public Guid? CategoryId { get; set; }

    [Inject]
    private PricelyApiClient Api { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    protected override Task OnInitializedAsync() => LoadCategoriesAsync();

    protected override async Task OnParametersSetAsync()
    {
        if (CategoryId == _loadedCategoryId)
        {
            return;
        }

        _loadedCategoryId = CategoryId;
        _message = null;
        SelectCategory();
        NewProduct();
        await LoadProductsAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        _categories = await CallAsync(() => Api.GetCategoriesAsync(CancellationToken.None)) ?? _categories;
        _tree = CategoryTree.DepthFirst(_categories);
        SelectCategory();
    }

    private void SelectCategory()
    {
        _category = CategoryId is Guid id ? _categories.FirstOrDefault(c => c.Id == id) : null;
        _categoryPath = _category is null ? null : CategoryPath.Build(_categories, _category.Id);
        _rename = _category?.Name ?? string.Empty;
    }

    private async Task LoadProductsAsync()
    {
        _products = null;
        if (_category is not null)
        {
            Guid categoryId = _category.Id;
            _products = await CallAsync(() => Api.GetProductsOfCategoryAsync(categoryId, CancellationToken.None)) ?? [];
        }
    }

    private Task CreateCategoryAsync() =>
        SaveAsync(async () =>
        {
            Guid? parentId = Guid.TryParse(_newCategoryParent, out Guid parent) ? parent : null;
            Guid created = await Api.CreateCategoryAsync(_newCategoryName.Trim(), parentId, CancellationToken.None);

            _newCategoryName = string.Empty;
            await LoadCategoriesAsync();
            Navigation.NavigateTo($"/catalog/{created}");

            return "Typologie créée.";
        });

    private Task RenameAsync() =>
        SaveAsync(async () =>
        {
            Guid categoryId = _category!.Id;
            await Api.RenameCategoryAsync(categoryId, _rename.Trim(), CancellationToken.None);
            await LoadCategoriesAsync();

            return "Typologie renommée.";
        });

    private Task SaveProductAsync() =>
        SaveAsync(async () =>
        {
            var product = new ProductRequest(
                _product.Name.Trim(),
                _product.Brand.Trim(),
                Optional(_product.Gtin),
                Optional(_product.ManufacturerReference),
                _product.CategoryId);

            if (_editedProductId is Guid productId)
            {
                await Api.UpdateProductAsync(productId, product, CancellationToken.None);
            }
            else
            {
                await Api.RegisterProductAsync(product, CancellationToken.None);
            }

            string done = _editedProductId is null ? "Produit ajouté." : "Produit enregistré.";
            NewProduct();
            await LoadProductsAsync();

            return done;
        });

    private void Edit(ProductDto product)
    {
        _editedProductId = product.Id;
        _message = null;
        _product = new ProductForm
        {
            Name = product.Name,
            Brand = product.Brand,
            Gtin = product.Gtin,
            ManufacturerReference = product.ManufacturerReference,
            CategoryId = product.ProductCategoryId
        };
    }

    private void NewProduct()
    {
        _editedProductId = null;
        _product = new ProductForm { CategoryId = _category?.Id ?? Guid.Empty };
    }

    /// <summary>One save at a time; a refused one shows the error of the API and keeps what was typed.</summary>
    private async Task SaveAsync(Func<Task<string>> save)
    {
        if (_saving)
        {
            return;
        }

        _saving = true;
        _message = null;
        Problem = null;

        try
        {
            _message = await CallAsync(save);
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>The level of a typology in a list (white space is collapsed in an option, not a no-break space).</summary>
    private static string Indent(int depth) => new('\u00A0', depth * 3);

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class ProductForm
    {
        public string Name { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public string? Gtin { get; set; }

        public string? ManufacturerReference { get; set; }

        public Guid CategoryId { get; set; }
    }
}
