using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Abstractions.Data;
using Pricely.Modules.Catalog.Domain.Categories;

namespace Pricely.Modules.Catalog.Application.Categories.RenameProductCategory;

internal sealed class RenameProductCategoryCommandHandler(
    IProductCategoryRepository productCategoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RenameProductCategoryCommand>
{
    public async Task<Result> Handle(RenameProductCategoryCommand request, CancellationToken cancellationToken)
    {
        ProductCategory? category = await productCategoryRepository.GetAsync(request.ProductCategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(ProductCategoryErrors.NotFound(request.ProductCategoryId));
        }

        Result result = category.Rename(request.Name);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
