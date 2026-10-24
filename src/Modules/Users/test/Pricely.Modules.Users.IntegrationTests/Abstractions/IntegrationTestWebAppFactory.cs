using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace Pricely.Modules.Users.IntegrationTests.Abstractions;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        Environment.SetEnvironmentVariable("ConnectionStrings:Database", _dbContainer.GetConnectionString());

        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "4df76c95-f617-4e27-94c4-04577ba022df");
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "4d107ffa-aee0-4b22-9dff-edcd6d5f44ae");
        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "qgD8Q~Gi2SX_5IpUGldRR.ews5cm~erCBfO6Vaeq");

    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
