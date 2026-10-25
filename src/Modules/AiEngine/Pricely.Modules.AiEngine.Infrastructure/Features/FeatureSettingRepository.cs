using Microsoft.EntityFrameworkCore;
using Pricely.Modules.AiEngine.Domain.Features;
using Pricely.Modules.AiEngine.Infrastructure.Database;

namespace Pricely.Modules.AiEngine.Infrastructure.Features;

internal sealed class FeatureSettingRepository(AiEngineDbContext context) : IFeatureSettingRepository
{
    public Task<FeatureSetting?> GetAsync(string featureCode, CancellationToken cancellationToken = default) =>
        context.FeatureSettings.FirstOrDefaultAsync(s => s.FeatureCode == featureCode, cancellationToken);

    public async Task<IReadOnlyCollection<FeatureSetting>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.FeatureSettings.AsNoTracking().ToListAsync(cancellationToken);

    public void Insert(FeatureSetting setting) => context.FeatureSettings.Add(setting);

    public void Remove(FeatureSetting setting) => context.FeatureSettings.Remove(setting);
}
