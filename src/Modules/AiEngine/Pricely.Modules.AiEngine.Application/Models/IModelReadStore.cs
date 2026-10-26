using Pricely.Modules.AiEngine.Application.Models.GetModels;

namespace Pricely.Modules.AiEngine.Application.Models;

/// <summary>
/// Read side of the model catalog (queries), implemented in Infrastructure.
/// </summary>
public interface IModelReadStore
{
    /// <summary>Every model with the price in force on the given day (UTC), by provider then label.</summary>
    Task<IReadOnlyCollection<ModelResponse>> GetAllAsync(DateTime todayUtc, CancellationToken cancellationToken = default);
}
