using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.Merchants.ConfigureMerchantSearch;

internal sealed class ConfigureMerchantSearchCommandHandler(IMerchantRepository merchantRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<ConfigureMerchantSearchCommand>
{
    public async Task<Result> Handle(ConfigureMerchantSearchCommand request, CancellationToken cancellationToken)
    {
        Merchant? merchant = await merchantRepository.GetAsync(request.MerchantId, cancellationToken);
        if (merchant is null)
        {
            return Result.Failure(MerchantErrors.NotFound(request.MerchantId));
        }

        Result result = merchant.ConfigureSearch(request.SearchTemplate);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
