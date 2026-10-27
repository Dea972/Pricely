using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.WebSearches;

namespace Pricely.Modules.AiEngine.Application.WebSearches.RecordWebSearch;

internal sealed class RecordWebSearchCommandHandler(
    WebSearchPriceOptions prices,
    IWebSearchRepository webSearchRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RecordWebSearchCommand>
{
    public async Task<Result> Handle(RecordWebSearchCommand request, CancellationToken cancellationToken)
    {
        decimal? pricePerSearch = prices.PricePerSearch.TryGetValue(request.Details.Provider, out decimal price) ? price : null;

        webSearchRepository.Insert(WebSearch.Record(request.Details, pricePerSearch));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
