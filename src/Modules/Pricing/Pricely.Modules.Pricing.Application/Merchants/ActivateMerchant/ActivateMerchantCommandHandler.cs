using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.Merchants.ActivateMerchant;

internal sealed class ActivateMerchantCommandHandler(IMerchantRepository merchantRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<ActivateMerchantCommand>
{
    public async Task<Result> Handle(ActivateMerchantCommand request, CancellationToken cancellationToken)
    {
        Merchant? merchant = await merchantRepository.GetAsync(request.MerchantId, cancellationToken);
        if (merchant is null)
        {
            return Result.Failure(MerchantErrors.NotFound(request.MerchantId));
        }

        merchant.Activate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
