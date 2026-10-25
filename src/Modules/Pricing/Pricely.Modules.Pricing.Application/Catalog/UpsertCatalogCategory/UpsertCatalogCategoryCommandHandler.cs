using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Catalog;

namespace Pricely.Modules.Pricing.Application.Catalog.UpsertCatalogCategory;

internal sealed class UpsertCatalogCategoryCommandHandler(
    ICatalogCategoryRepository catalogCategoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpsertCatalogCategoryCommand>
{
    public async Task<Result> Handle(UpsertCatalogCategoryCommand request, CancellationToken cancellationToken)
    {
        CatalogCategory? category = await catalogCategoryRepository.GetAsync(request.ProductCategoryId, cancellationToken);

        if (category is null)
        {
            catalogCategoryRepository.Insert(
                CatalogCategory.Create(request.ProductCategoryId, request.Name, request.ParentId));
        }
        else
        {
            category.Rename(request.Name);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
