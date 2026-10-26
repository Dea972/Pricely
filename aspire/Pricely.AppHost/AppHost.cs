IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<PostgresDatabaseResource> db = postgres.AddDatabase("pricely-db");

// Fixed port so that the issuer of the tokens (http://localhost:8180/realms/pricely) is stable (ADR-010). Not 8080: too
// common, another process on [::1]:8080 (WSL2 relay, a local app) answers localhost instead of Keycloak.
// Not persistent: the realm is imported only on the first start of a container, so each run re-imports the versioned file.
// Plain HTTP: with a trusted developer certificate, Aspire would switch the endpoint to https while Keycloak still
// answers HTTP on 8180 (the front and the API then fail to read https://localhost:8180/.../openid-configuration), and
// the issuer would no longer be the one of the MCP clients.
// WithoutHttpsCertificate is still an evaluation API of Aspire 13.6 (ASPIRECERTIFICATES001).
#pragma warning disable ASPIRECERTIFICATES001
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", 8180)
    .WithoutHttpsCertificate()
    .WithRealmImport("./Realms");
#pragma warning restore ASPIRECERTIFICATES001

// Brings every module schema up to date, then exits: the API starts only once it has completed (ADR-018).
IResourceBuilder<ProjectResource> migrations = builder.AddProject<Projects.Pricely_MigrationService>("pricely-migrations")
    .WithReference(db)
    .WaitFor(db);

// Local test data (typologies, products, merchants, 13 weeks of prices): started from the dashboard (▶), never on its own.
// Running it again adds nothing.
builder.AddProject<Projects.Pricely_DevSeed>("pricely-seed")
    .WithReference(db)
    .WaitForCompletion(migrations)
    .WithExplicitStart();

IResourceBuilder<ProjectResource> api = builder.AddProject<Projects.Pricely_Api>("Pricely-api")
    .WithReference(db)
    .WaitForCompletion(migrations)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/pricely"));

// Secret parameter, never in the repository:
//   dotnet user-secrets set "Parameters:anthropic-api-key" "<key>" --project aspire/Pricely.AppHost
// Optional: without it the API still starts and only the calls to the LLM fail.
if (!string.IsNullOrEmpty(builder.Configuration["Parameters:anthropic-api-key"]))
{
    api.WithEnvironment("Ai__Anthropic__ApiKey", builder.AddParameter("anthropic-api-key", secret: true));
}

// The front (ADR-015): signs the user in with Keycloak (client pricely-web) and calls the API with the user's token.
builder.AddProject<Projects.Pricely_Web>("pricely-web")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("Authentication__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/pricely"))
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
