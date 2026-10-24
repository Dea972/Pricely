using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Domain.Categories;

/// <summary>
/// Product typology. Categories form a tree limited to <see cref="MaxDepth"/> levels
/// (for example: Gros œuvre &gt; Ciment &gt; Ciment prompt).
/// </summary>
public sealed class ProductCategory : Entity
{
    public const int MaxDepth = 3;

    public const int NameMaxLength = 200;

    private ProductCategory()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Guid? ParentId { get; private set; }

    public int Depth { get; private set; }

    public static Result<ProductCategory> CreateRoot(string name)
    {
        Result nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
        {
            return Result.Failure<ProductCategory>(nameValidation.Error);
        }

        return Create(name, parentId: null, depth: 1);
    }

    public static Result<ProductCategory> CreateChild(string name, ProductCategory parent)
    {
        Result nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
        {
            return Result.Failure<ProductCategory>(nameValidation.Error);
        }

        if (parent.Depth >= MaxDepth)
        {
            return Result.Failure<ProductCategory>(ProductCategoryErrors.MaxDepthExceeded(parent.Id));
        }

        return Create(name, parent.Id, parent.Depth + 1);
    }

    public Result Rename(string name)
    {
        Result nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
        {
            return nameValidation;
        }

        string trimmedName = name.Trim();
        if (string.Equals(Name, trimmedName, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        Name = trimmedName;

        Raise(new ProductCategoryRenamedDomainEvent(Id, Name));

        return Result.Success();
    }

    private static ProductCategory Create(string name, Guid? parentId, int depth)
    {
        ProductCategory category = new()
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            ParentId = parentId,
            Depth = depth
        };

        category.Raise(new ProductCategoryCreatedDomainEvent(category.Id));

        return category;
    }

    private static Result ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            return Result.Failure(ProductCategoryErrors.InvalidName);
        }

        return Result.Success();
    }
}
