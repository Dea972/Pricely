using FluentValidation;
using Pricely.Modules.Catalog.Domain.Categories;

namespace Pricely.Modules.Catalog.Application.Categories.CreateProductCategory;

internal sealed class CreateProductCategoryCommandValidator : AbstractValidator<CreateProductCategoryCommand>
{
    public CreateProductCategoryCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(ProductCategory.NameMaxLength);
        RuleFor(c => c.ParentId).NotEqual(Guid.Empty);
    }
}
