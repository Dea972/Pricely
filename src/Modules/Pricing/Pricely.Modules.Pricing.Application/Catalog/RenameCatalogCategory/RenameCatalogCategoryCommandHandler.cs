using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Catalog;

namespace Pricely.Modules.Pricing.Application.Catalog.RenameCatalogCategory;

internal sealed class RenameCatalogCategoryCommandHandler(
    ICatalogCategoryRepository catalogCategoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RenameCatalogCategoryCommand>
{
    public async Task<Result> Handle(RenameCatalogCategoryCommand request, CancellationToken cancellationToken)
    {
        CatalogCategory? category = await catalogCategoryRepository.GetAsync(request.ProductCategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(CatalogErrors.CategoryNotFound(request.ProductCategoryId));
        }

        category.Rename(request.Name);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
