IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Secret parameter, set with: dotnet user-secrets set "Parameters:anthropic-api-key" "<key>" --project aspire/Pricely.AppHost
IResourceBuilder<ParameterResource> anthropicApiKey = builder.AddParameter("anthropic-api-key", secret: true);

IResourceBuilder<SqlServerServerResource> mssql = builder.AddSqlServer("mssql")
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<SqlServerDatabaseResource> db = mssql.AddDatabase("pricely-db");

// Fixed port so that the issuer of the tokens (http://localhost:8080/realms/pricely) is stable (ADR-010).
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithRealmImport("./Realms")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.Pricely_Api>("Pricely-api")
    .WithReference(db)
    .WaitFor(db)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/pricely"))
    .WithEnvironment("Ai__Anthropic__ApiKey", anthropicApiKey);

await builder.Build().RunAsync();
