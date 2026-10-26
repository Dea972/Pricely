using Pricely.Web.Api;

namespace Pricely.Web.Components.Shared;

/// <summary>The typologies in reading order: each one followed by its children, by name.</summary>
public static class CategoryTree
{
    /// <summary>Catalog allows three levels: a typology of the third level takes no child.</summary>
    public const int MaxDepth = 3;

    public static List<(ProductCategoryDto Category, int Depth)> DepthFirst(IReadOnlyList<ProductCategoryDto> categories)
    {
        ILookup<Guid?, ProductCategoryDto> children = categories.ToLookup(c => c.ParentId);
        var ordered = new List<(ProductCategoryDto, int)>();
        var known = categories.Select(c => c.Id).ToHashSet();

        void Visit(ProductCategoryDto category, int depth)
        {
            ordered.Add((category, depth));
            foreach (ProductCategoryDto child in children[category.Id].OrderBy(c => c.Name, StringComparer.CurrentCulture))
            {
                Visit(child, depth + 1);
            }
        }

        // Roots, and categories whose parent is unknown (never hidden).
        foreach (ProductCategoryDto root in categories.Where(c => c.ParentId is null || !known.Contains(c.ParentId.Value)).OrderBy(c => c.Name, StringComparer.CurrentCulture))
        {
            Visit(root, 0);
        }

        return ordered;
    }
}
