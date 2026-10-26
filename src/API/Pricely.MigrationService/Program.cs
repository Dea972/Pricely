using Pricely.Infrastructure;

// Brings every module schema up to date, then exits (ADR-018). The AppHost starts the API only once it has completed;
// outside the AppHost, run it before each deployment of the API, with the same connection string.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

string connectionString = builder.Configuration.GetConnectionString("pricely-db")
    ?? throw new InvalidOperationException("The connection string 'pricely-db' is not configured.");

// The same composition as the API, so that each DbContext is configured exactly as it runs. The host is never started:
// no background job (outbox, inbox, collection) runs here.
await builder.Services.AddPricelyAsync(connectionString, builder.Configuration);

// The HTTP services of the composition (authorization, endpoints) need ASP.NET Core routing, absent from a worker: they
// are never resolved here, so they are not validated (the API validates the whole container at its start).
builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = true }));

using IHost host = builder.Build();

ILogger logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Pricely.MigrationService");
logger.LogInformation("Applying the migrations of every module");

await host.Services.ApplyMigrationsAsync();

logger.LogInformation("Every module schema is up to date");
