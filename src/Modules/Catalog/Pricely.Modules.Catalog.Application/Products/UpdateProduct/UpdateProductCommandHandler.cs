using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Abstractions.Data;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Application.Products.UpdateProduct;

internal sealed class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IProductCategoryRepository productCategoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProductCommand>
{
    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository.GetAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(request.ProductId));
        }

        ProductCategory? category = await productCategoryRepository.GetAsync(request.ProductCategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(ProductCategoryErrors.NotFound(request.ProductCategoryId));
        }

        Result<ProductDescription> description = ProductDescriptionFactory.Create(
            request.Name,
            request.Brand,
            request.Gtin,
            request.ManufacturerReference);
        if (description.IsFailure)
        {
            return description;
        }

        Gtin? gtin = description.Value.Gtin;
        if (gtin is not null && await productRepository.ExistsWithGtinAsync(gtin, product.Id, cancellationToken))
        {
            return Result.Failure(ProductErrors.GtinAlreadyUsed(gtin.Value));
        }

        Result result = product.Update(description.Value, category.Id);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
