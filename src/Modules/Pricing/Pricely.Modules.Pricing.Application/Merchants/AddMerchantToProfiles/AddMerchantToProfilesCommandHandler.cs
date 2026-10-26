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
        if (await merchantRepository.GetAsync(request.MerchantId, cancellationToken) is null)
        {
            return Result.Failure(MerchantErrors.NotFound(request.MerchantId));
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
        IReadOnlyList<Guid> current;
        if (own is { PreferredMerchants.Count: > 0 })
        {
            current = [.. own.PreferredMerchants.Select(m => m.MerchantId)];
        }
        else
        {
            Result<ResolvedCollectionProfile> inherited = await CollectionProfileLookup.ResolveAsync(
                categoryId,
                catalogCategoryRepository,
                collectionProfileRepository,
                cancellationToken);
            if (inherited.IsFailure)
            {
                return inherited;
            }

            current = inherited.Value.PreferredMerchantIds;
        }

        if (current.Contains(merchantId))
        {
            return Result.Success();
        }

        Guid[] merchants = [.. current, merchantId];
        if (own is null)
        {
            Result<CollectionProfile> defined = CollectionProfile.Define(categoryId, merchants, instructions: null);
            if (defined.IsFailure)
            {
                return defined;
            }

            collectionProfileRepository.Insert(defined.Value);
        }
        else
        {
            Result updated = own.Update(merchants, own.Instructions);
            if (updated.IsFailure)
            {
                return updated;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
