using Microsoft.EntityFrameworkCore;
using Pricely.Modules.AiEngine.Domain.Models;
using Pricely.Modules.AiEngine.Infrastructure.Database;

namespace Pricely.Modules.AiEngine.Infrastructure.Models;

internal sealed class ModelRepository(AiEngineDbContext context) : IModelRepository
{
    public Task<Model?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Models.Include(m => m.Prices).FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    // provider_model_id is citext: the comparison ignores case.
    public Task<Model?> FindAsync(Provider provider, string providerModelId, CancellationToken cancellationToken = default) =>
        context.Models
            .Include(m => m.Prices)
            .FirstOrDefaultAsync(m => m.Provider == provider && m.ProviderModelId == providerModelId.Trim(), cancellationToken);

    public Task<bool> ExistsAsync(Provider provider, string providerModelId, CancellationToken cancellationToken = default) =>
        context.Models.AnyAsync(m => m.Provider == provider && m.ProviderModelId == providerModelId, cancellationToken);

    public void Insert(Model model) => context.Models.Add(model);
}
