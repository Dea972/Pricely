IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<PostgresDatabaseResource> db = postgres.AddDatabase("pricely-db");

// Fixed port so that the issuer of the tokens (http://localhost:8080/realms/pricely) is stable (ADR-010).
// Not persistent: the realm is imported only on the first start of a container, so each run re-imports the versioned file.
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithRealmImport("./Realms");

IResourceBuilder<ProjectResource> api = builder.AddProject<Projects.Pricely_Api>("Pricely-api")
    .WithReference(db)
    .WaitFor(db)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/pricely"));

// Secret parameter, never in the repository:
//   dotnet user-secrets set "Parameters:anthropic-api-key" "<key>" --project aspire/Pricely.AppHost
// Optional: without it the API still starts and only the calls to the LLM fail.
if (!string.IsNullOrEmpty(builder.Configuration["Parameters:anthropic-api-key"]))
{
    api.WithEnvironment("Ai__Anthropic__ApiKey", builder.AddParameter("anthropic-api-key", secret: true));
}

await builder.Build().RunAsync();
