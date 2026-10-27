using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.Merchants.FindOrRegisterMerchant;

internal sealed class FindOrRegisterMerchantCommandHandler(IMerchantRepository merchantRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<FindOrRegisterMerchantCommand, FoundMerchantResponse>
{
    public async Task<Result<FoundMerchantResponse>> Handle(FindOrRegisterMerchantCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Merchant> merchants = await merchantRepository.GetAllAsync(cancellationToken);

        Merchant? existing = merchants.FirstOrDefault(m => m.Owns(request.Website));
        if (existing is not null)
        {
            return From(existing, isNew: false);
        }

        string name = request.Name.Trim();
        Merchant? sameName = merchants.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        if (sameName is not null)
        {
            return Result.Failure<FoundMerchantResponse>(MerchantErrors.NameUsedByAnotherSite(name, sameName.Website));
        }

        // The site, not the page that was found on it.
        Result<Merchant> merchant = Merchant.Register(name, request.Website.GetLeftPart(UriPartial.Authority) + "/");
        if (merchant.IsFailure)
        {
            return Result.Failure<FoundMerchantResponse>(merchant.Error);
        }

        merchantRepository.Insert(merchant.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return From(merchant.Value, isNew: true);
    }

    private static FoundMerchantResponse From(Merchant merchant, bool isNew) =>
        new(merchant.Id, merchant.Name, merchant.Website, merchant.IsActive, isNew);
}
