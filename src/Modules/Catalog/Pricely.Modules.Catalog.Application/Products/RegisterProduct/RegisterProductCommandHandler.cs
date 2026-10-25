using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Abstractions.Data;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Application.Products.RegisterProduct;

internal sealed class RegisterProductCommandHandler(
    IProductRepository productRepository,
    IProductCategoryRepository productCategoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterProductCommand request, CancellationToken cancellationToken)
    {
        ProductCategory? category = await productCategoryRepository.GetAsync(request.ProductCategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<Guid>(ProductCategoryErrors.NotFound(request.ProductCategoryId));
        }

        Result<ProductDescription> description = ProductDescriptionFactory.Create(
            request.Name,
            request.Brand,
            request.Gtin,
            request.ManufacturerReference);
        if (description.IsFailure)
        {
            return Result.Failure<Guid>(description.Error);
        }

        Gtin? gtin = description.Value.Gtin;
        if (gtin is not null && await productRepository.ExistsWithGtinAsync(gtin, null, cancellationToken))
        {
            return Result.Failure<Guid>(ProductErrors.GtinAlreadyUsed(gtin.Value));
        }

        Result<Product> product = Product.Register(description.Value, category.Id);
        if (product.IsFailure)
        {
            return Result.Failure<Guid>(product.Error);
        }

        productRepository.Insert(product.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Value.Id;
    }
}
