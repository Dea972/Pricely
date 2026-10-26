using Pricely.DevSeed;
using Pricely.Infrastructure;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;

// Local test data (never in production): typologies, products, merchants, collection profiles, then 13 weeks of price
// observations. Everything goes through the commands of the modules, so the matching policy and the price guard decide
// what is confirmed, proposed or flagged. Run it once the schemas are up to date (Pricely.MigrationService); running it
// again adds nothing.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// It replaces the collector and writes fictitious merchants: never on a database other than a developer's one.
if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Pricely.DevSeed only runs in the Development environment (DOTNET_ENVIRONMENT=Development).");
}

string connectionString = builder.Configuration.GetConnectionString("pricely-db")
    ?? throw new InvalidOperationException("The connection string 'pricely-db' is not configured.");

// The collection reads the scripted pages of the test data instead of the merchant sites. Registered before the modules:
// Pricing only adds its own collector when none is registered.
builder.Services.AddSingleton<SeedOfferCollector>();
builder.Services.AddScoped<IOfferCollector>(serviceProvider => serviceProvider.GetRequiredService<SeedOfferCollector>());

await builder.Services.AddPricelyAsync(connectionString, builder.Configuration);
builder.Services.AddSingleton<SeedRunner>();

// The HTTP services of the composition need ASP.NET Core routing, absent from a worker: they are never resolved here.
builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = true }));

using IHost host = builder.Build();

// Started: the outbox and inbox jobs carry the Catalog events to the read models of Pricing, as in the API.
await host.StartAsync();

try
{
    await host.Services.GetRequiredService<SeedRunner>().RunAsync(CancellationToken.None);
}
finally
{
    await host.StopAsync();
}
