using Microsoft.Extensions.Options;
using Quartz;

namespace Pricely.Modules.Pricing.Infrastructure.Collection;

internal sealed class ConfigureCollectOffersJob(IOptions<CollectionOptions> collectionOptions)
    : IConfigureOptions<QuartzOptions>
{
    private readonly CollectionOptions _collectionOptions = collectionOptions.Value;

    public void Configure(QuartzOptions options)
    {
        if (!_collectionOptions.Enabled)
        {
            return;
        }

        string jobName = typeof(CollectOffersJob).FullName!;

        options
            .AddJob<CollectOffersJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithSimpleSchedule(schedule =>
                        schedule.WithIntervalInMinutes(_collectionOptions.IntervalInMinutes).RepeatForever()));
    }
}
