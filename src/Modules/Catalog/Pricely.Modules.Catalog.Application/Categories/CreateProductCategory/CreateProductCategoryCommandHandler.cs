using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Abstractions.Data;
using Pricely.Modules.Catalog.Domain.Categories;

namespace Pricely.Modules.Catalog.Application.Categories.CreateProductCategory;

internal sealed class CreateProductCategoryCommandHandler(
    IProductCategoryRepository productCategoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCategoryCommand request, CancellationToken cancellationToken)
    {
        Result<ProductCategory> result;

        if (request.ParentId is null)
        {
            result = ProductCategory.CreateRoot(request.Name);
        }
        else
        {
            ProductCategory? parent = await productCategoryRepository.GetAsync(request.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<Guid>(ProductCategoryErrors.NotFound(request.ParentId.Value));
            }

            result = ProductCategory.CreateChild(request.Name, parent);
        }

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        productCategoryRepository.Insert(result.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
