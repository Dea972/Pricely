using Pricely.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Fail at startup, not on the first request, when a registration is missing or a scoped service is captured by a singleton
// (scoped services, keyed LLM providers, MassTransit consumers, Quartz jobs).
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Provided by the Aspire AppHost (WithReference(db)); with `dotnet ef`, set ConnectionStrings__pricely-db.
string connectionString = builder.Configuration.GetConnectionString("pricely-db")
    ?? throw new InvalidOperationException("The connection string 'pricely-db' is not configured.");

await builder.Services.AddPricelyAsync(connectionString, builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // The local database is created empty by the AppHost: bring every module schema up to date.
    await app.Services.ApplyMigrationsAsync();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapPricelyEndpoints();

await app.RunAsync();
