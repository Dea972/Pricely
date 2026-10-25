using Microsoft.EntityFrameworkCore;
using Pricely.Common.Application.AI;
using Pricely.Modules.AiEngine.Domain.Features;
using Pricely.Modules.AiEngine.Domain.Models;
using Pricely.Modules.AiEngine.Infrastructure.Database;

namespace Pricely.Modules.AiEngine.Infrastructure.Features;

/// <summary>
/// Target of a feature as chosen by administrators, read on every call: a few milliseconds next to an LLM call,
/// and a change applies immediately. Replaces the default resolver of Common.
/// </summary>
internal sealed class LlmTargetResolver(AiEngineDbContext context) : ILlmTargetResolver
{
    public async Task<LlmTarget?> ResolveAsync(AiFeature feature, CancellationToken cancellationToken = default)
    {
        List<FeatureSetting> settings = await context.FeatureSettings
            .AsNoTracking()
            .Where(s => s.FeatureCode == feature.Code || s.FeatureCode == FeatureSetting.DefaultCode)
            .ToListAsync(cancellationToken);

        var effective = FeatureSetting.Effective(
            settings.Find(s => string.Equals(s.FeatureCode, feature.Code, StringComparison.OrdinalIgnoreCase)),
            settings.Find(s => s.IsDefault));

        if (effective is null)
        {
            return null;
        }

        Model? model = await context.Models
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == effective.ModelId, cancellationToken);

        return model is null ? null : new LlmTarget(ProviderKey(model.Provider), model.ProviderModelId);
    }

    /// <summary>Keyed service of the provider (see <see cref="AiProviderKeys"/>).</summary>
    private static string ProviderKey(Provider provider) => provider switch
    {
        Provider.Anthropic => AiProviderKeys.Anthropic,
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown provider")
    };
}
