IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<PostgresDatabaseResource> db = postgres.AddDatabase("pricely-db");

// Fixed port so that the issuer of the tokens (http://localhost:8080/realms/pricely) is stable (ADR-010).
// Not persistent: the realm is imported only on the first start of a container, so each run re-imports the versioned file.
// Plain HTTP: with a trusted developer certificate, Aspire would switch the endpoint to https while Keycloak still
// answers HTTP on 8080 (the front and the API then fail to read https://localhost:8080/.../openid-configuration), and
// the issuer would no longer be the one of the MCP clients.
// WithoutHttpsCertificate is still an evaluation API of Aspire 13.6 (ASPIRECERTIFICATES001).
#pragma warning disable ASPIRECERTIFICATES001
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithoutHttpsCertificate()
    .WithRealmImport("./Realms");
#pragma warning restore ASPIRECERTIFICATES001

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

// The front (ADR-015): signs the user in with Keycloak (client pricely-web) and calls the API with the user's token.
builder.AddProject<Projects.Pricely_Web>("pricely-web")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("Authentication__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/pricely"))
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
