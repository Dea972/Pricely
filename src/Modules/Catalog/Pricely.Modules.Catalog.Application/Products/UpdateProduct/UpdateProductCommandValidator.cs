using FluentValidation;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Application.Products.UpdateProduct;

internal sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(c => c.ProductId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(c => c.Brand).NotEmpty().MaximumLength(Product.BrandMaxLength);
        RuleFor(c => c.ManufacturerReference).MaximumLength(Product.ManufacturerReferenceMaxLength);
        RuleFor(c => c.ProductCategoryId).NotEmpty();
    }
}
