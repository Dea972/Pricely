using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.CollectionProfiles.DefineCollectionProfile;

internal sealed class DefineCollectionProfileCommandHandler(
    IProductCategoryReader productCategoryReader,
    ICollectionProfileRepository collectionProfileRepository,
    IMerchantRepository merchantRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DefineCollectionProfileCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DefineCollectionProfileCommand request, CancellationToken cancellationToken)
    {
        Result<ProductCategorySummary?> category =
            await productCategoryReader.GetAsync(request.ProductCategoryId, cancellationToken);
        if (category.IsFailure)
        {
            return Result.Failure<Guid>(category.Error);
        }

        if (category.Value is null)
        {
            return Result.Failure<Guid>(CatalogErrors.CategoryNotFound(request.ProductCategoryId));
        }

        Result merchantsCheck = await EnsureMerchantsExistAsync(request.PreferredMerchantIds, cancellationToken);
        if (merchantsCheck.IsFailure)
        {
            return Result.Failure<Guid>(merchantsCheck.Error);
        }

        CollectionProfile? profile =
            await collectionProfileRepository.GetByCategoryAsync(request.ProductCategoryId, cancellationToken);

        if (profile is not null)
        {
            Result update = profile.Update(request.PreferredMerchantIds, request.Instructions);
            if (update.IsFailure)
            {
                return Result.Failure<Guid>(update.Error);
            }
        }
        else
        {
            Result<CollectionProfile> definition = CollectionProfile.Define(
                request.ProductCategoryId,
                request.PreferredMerchantIds,
                request.Instructions);
            if (definition.IsFailure)
            {
                return Result.Failure<Guid>(definition.Error);
            }

            profile = definition.Value;
            collectionProfileRepository.Insert(profile);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.Id;
    }

    private async Task<Result> EnsureMerchantsExistAsync(
        IReadOnlyList<Guid> merchantIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Merchant> merchants = await merchantRepository.GetManyAsync(merchantIds, cancellationToken);
        HashSet<Guid> knownMerchantIds = [.. merchants.Select(m => m.Id)];

        foreach (Guid merchantId in merchantIds)
        {
            if (!knownMerchantIds.Contains(merchantId))
            {
                return Result.Failure(MerchantErrors.NotFound(merchantId));
            }
        }

        return Result.Success();
    }
}
