using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Domain.Categories;

public static class ProductCategoryErrors
{
    public static readonly Error InvalidName = Error.Problem(
        "ProductCategories.InvalidName",
        $"The category name is required and cannot exceed {ProductCategory.NameMaxLength} characters");

    public static Error NotFound(Guid categoryId) =>
        Error.NotFound("ProductCategories.NotFound", $"The product category with the identifier {categoryId} was not found");

    public static Error MaxDepthExceeded(Guid parentId) =>
        Error.Problem(
            "ProductCategories.MaxDepthExceeded",
            $"The category {parentId} is already at the maximum depth of {ProductCategory.MaxDepth} levels");
}
