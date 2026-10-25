using Microsoft.Extensions.DependencyInjection;

namespace Pricely.Common.Application.AI;

public static class AiFeatureServiceCollectionExtensions
{
    /// <summary>
    /// Declares an AI feature of a module. Declared features are listed (and can be targeted) by the AiEngine module.
    /// </summary>
    public static IServiceCollection AddAiFeature(this IServiceCollection services, AiFeature feature) =>
        services.AddSingleton(feature);
}
