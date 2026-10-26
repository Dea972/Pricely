using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Application.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.Merchants.AddMerchantToProfiles;

/// <summary>One profile per save (one aggregate per transaction): a typology already done stays done on a failure.</summary>
internal sealed class AddMerchantToProfilesCommandHandler(
    IMerchantRepository merchantRepository,
    ICatalogCategoryRepository catalogCategoryRepository,
    ICollectionProfileRepository collectionProfileRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddMerchantToProfilesCommand>
{
    public async Task<Result> Handle(AddMerchantToProfilesCommand request, CancellationToken cancellationToken)
    {
        Merchant? merchant = await merchantRepository.GetAsync(request.MerchantId, cancellationToken);
        if (merchant is null)
        {
            return Result.Failure(MerchantErrors.NotFound(request.MerchantId));
        }

        if (!merchant.IsActive)
        {
            return Result.Failure(MerchantErrors.Inactive(request.MerchantId));
        }

        foreach (Guid categoryId in request.ProductCategoryIds.Distinct())
        {
            Result added = await AddAsync(request.MerchantId, categoryId, cancellationToken);
            if (added.IsFailure)
            {
                return added;
            }
        }

        return Result.Success();
    }

    private async Task<Result> AddAsync(Guid merchantId, Guid categoryId, CancellationToken cancellationToken)
    {
        if (await catalogCategoryRepository.GetAsync(categoryId, cancellationToken) is null)
        {
            return Result.Failure(CatalogErrors.CategoryNotFound(categoryId));
        }

        CollectionProfile? own = await collectionProfileRepository.GetByCategoryAsync(categoryId, cancellationToken);
        IReadOnlyList<Guid> inherited = [];
        if (own is not { PreferredMerchants.Count: > 0 })
        {
            Result<ResolvedCollectionProfile> resolved = await CollectionProfileLookup.ResolveAsync(
                categoryId,
                catalogCategoryRepository,
                collectionProfileRepository,
                cancellationToken);
            if (resolved.IsFailure)
            {
                return resolved;
            }

            inherited = resolved.Value.PreferredMerchantIds;
        }

        if (own is null)
        {
            Result<CollectionProfile> defined = CollectionProfile.Define(categoryId, inherited.Contains(merchantId) ? inherited : [.. inherited, merchantId], instructions: null);
            if (defined.IsFailure)
            {
                return defined;
            }

            collectionProfileRepository.Insert(defined.Value);
        }
        else
        {
            Result added = own.AddPreferredMerchant(merchantId, inherited);
            if (added.IsFailure)
            {
                return added;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
