using Pricely.Web.Api;

namespace Pricely.Web.Components.Shared;

/// <summary>
/// Products and categories already read during the circuit: the cards of a conversation name the same product many times.
/// </summary>
internal sealed class ProductLookup(PricelyApiClient api)
{
    private readonly Dictionary<Guid, Task<ProductDto?>> _products = [];
    private Task<IReadOnlyList<ProductCategoryDto>>? _categories;

    public Task<ProductDto?> FindAsync(Guid productId)
    {
        if (!_products.TryGetValue(productId, out Task<ProductDto?>? product) || product.IsFaulted)
        {
            product = LoadAsync(productId);
            _products[productId] = product;
        }

        return product;
    }

    /// <summary>« Gros œuvre › Ciment », from the root to the category.</summary>
    public async Task<string?> CategoryPathAsync(Guid categoryId)
    {
        if (_categories is null || _categories.IsFaulted)
        {
            _categories = api.GetCategoriesAsync(CancellationToken.None);
        }

        return CategoryPath.Build(await _categories, categoryId);
    }

    private async Task<ProductDto?> LoadAsync(Guid productId)
    {
        try
        {
            return await api.GetProductAsync(productId, CancellationToken.None);
        }
        catch (PricelyApiException)
        {
            // Unknown or removed product: the card shows the call without its name.
            return null;
        }
    }
}

public static class CategoryPath
{
    public const string Separator = " › ";

    public static string? Build(IReadOnlyList<ProductCategoryDto> categories, Guid categoryId)
    {
        var byId = categories.ToDictionary(c => c.Id);
        var names = new List<string>();
        Guid? current = categoryId;

        // The tree has three levels at most; the bound only guards against a cycle in bad data.
        while (current is Guid id && byId.TryGetValue(id, out ProductCategoryDto? category) && names.Count <= categories.Count)
        {
            names.Insert(0, category.Name);
            current = category.ParentId;
        }

        return names.Count == 0 ? null : string.Join(Separator, names);
    }
}
