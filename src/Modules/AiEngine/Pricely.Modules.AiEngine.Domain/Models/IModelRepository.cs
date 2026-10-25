namespace Pricely.Modules.AiEngine.Domain.Models;

public interface IModelRepository
{
    /// <summary>Model with its prices.</summary>
    Task<Model?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Model (with its prices) whose provider and identifier match the ones reported by a call, ignoring case.</summary>
    Task<Model?> FindAsync(Provider provider, string providerModelId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Provider provider, string providerModelId, CancellationToken cancellationToken = default);

    void Insert(Model model);
}
