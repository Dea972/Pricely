using FluentValidation;
using Pricely.Modules.Catalog.Domain.Categories;

namespace Pricely.Modules.Catalog.Application.Categories.RenameProductCategory;

internal sealed class RenameProductCategoryCommandValidator : AbstractValidator<RenameProductCategoryCommand>
{
    public RenameProductCategoryCommandValidator()
    {
        RuleFor(c => c.ProductCategoryId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(ProductCategory.NameMaxLength);
    }
}
