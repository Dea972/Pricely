using FluentValidation;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Application.Products.FindOrRegisterProduct;

internal sealed class FindOrRegisterProductCommandValidator : AbstractValidator<FindOrRegisterProductCommand>
{
    public FindOrRegisterProductCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(c => c.Brand).NotEmpty().MaximumLength(Product.BrandMaxLength);
        RuleFor(c => c.ManufacturerReference).MaximumLength(Product.ManufacturerReferenceMaxLength);
        RuleFor(c => c.CategoryPath).NotNull().Must(p => p is { Count: >= 1 and <= ProductCategory.MaxDepth })
            .WithMessage($"Give the typology as 1 to {ProductCategory.MaxDepth} names, from the root");
        RuleForEach(c => c.CategoryPath).NotEmpty().MaximumLength(ProductCategory.NameMaxLength);
    }
}
