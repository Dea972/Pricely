using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.Merchants.RegisterMerchant;

internal sealed class RegisterMerchantCommandHandler(IMerchantRepository merchantRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterMerchantCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterMerchantCommand request, CancellationToken cancellationToken)
    {
        Result<Merchant> merchant = Merchant.Register(request.Name, request.Website, request.SearchTemplate);
        if (merchant.IsFailure)
        {
            return Result.Failure<Guid>(merchant.Error);
        }

        merchantRepository.Insert(merchant.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return merchant.Value.Id;
    }
}
