IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);


IResourceBuilder<SqlServerServerResource> mssql = builder.AddSqlServer("mssql")
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<SqlServerDatabaseResource> db = mssql.AddDatabase("pricely-db");

builder.AddProject<Projects.Pricely_Api>("Pricely-api")
    .WithReference(db)
    .WaitFor(db);


await builder.Build().RunAsync();
