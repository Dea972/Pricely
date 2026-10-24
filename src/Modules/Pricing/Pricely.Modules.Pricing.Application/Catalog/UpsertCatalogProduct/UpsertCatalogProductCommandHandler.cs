using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Catalog;

namespace Pricely.Modules.Pricing.Application.Catalog.UpsertCatalogProduct;

internal sealed class UpsertCatalogProductCommandHandler(
    ICatalogProductRepository catalogProductRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpsertCatalogProductCommand>
{
    public async Task<Result> Handle(UpsertCatalogProductCommand request, CancellationToken cancellationToken)
    {
        CatalogProduct? product = await catalogProductRepository.GetAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            catalogProductRepository.Insert(CatalogProduct.Create(request.ProductId, request.Snapshot));
        }
        else
        {
            product.Update(request.Snapshot);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
