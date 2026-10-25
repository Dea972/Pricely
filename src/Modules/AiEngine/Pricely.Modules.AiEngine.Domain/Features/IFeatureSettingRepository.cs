namespace Pricely.Modules.AiEngine.Domain.Features;

public interface IFeatureSettingRepository
{
    Task<FeatureSetting?> GetAsync(string featureCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FeatureSetting>> GetAllAsync(CancellationToken cancellationToken = default);

    void Insert(FeatureSetting setting);

    void Remove(FeatureSetting setting);
}
