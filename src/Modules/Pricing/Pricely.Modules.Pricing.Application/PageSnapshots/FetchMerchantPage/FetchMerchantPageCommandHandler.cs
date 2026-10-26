using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Application.PageSnapshots.FetchMerchantPage;

internal sealed class FetchMerchantPageCommandHandler(MerchantPageReader pageReader)
    : ICommandHandler<FetchMerchantPageCommand, FetchedPageResponse>
{
    public async Task<Result<FetchedPageResponse>> Handle(FetchMerchantPageCommand request, CancellationToken cancellationToken)
    {
        Result<PageSnapshot> snapshot = await pageReader.ReadAsync(request.MerchantId, request.Address, request.SearchText, cancellationToken);

        return snapshot.IsFailure
            ? Result.Failure<FetchedPageResponse>(snapshot.Error)
            : FetchedPageResponse.From(snapshot.Value);
    }
}
