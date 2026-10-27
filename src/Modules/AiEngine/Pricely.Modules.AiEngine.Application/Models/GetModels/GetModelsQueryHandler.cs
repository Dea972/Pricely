using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Application.Models.GetModels;

internal sealed class GetModelsQueryHandler(IModelReadStore readStore, IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetModelsQuery, IReadOnlyCollection<ModelResponse>>
{
    public async Task<Result<IReadOnlyCollection<ModelResponse>>> Handle(
        GetModelsQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetAllAsync(dateTimeProvider.UtcNow, cancellationToken));
}
