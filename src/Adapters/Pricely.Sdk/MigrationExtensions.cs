using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Agents.Core.Infrastructure;
using Pricely.Modules.AiEngine.Infrastructure.Database;
using Pricely.Modules.Catalog.Infrastructure.Database;
using Pricely.Modules.Pricing.Infrastructure.Database;
using Pricely.Modules.Users.Infrastructure.Database;

namespace Pricely.Infrastructure;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies the pending migrations of every module. Each module owns its schema and its own migrations history table,
    /// so the order does not matter.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();

        await MigrateAsync<CatalogDbContext>(scope);
        await MigrateAsync<PricingDbContext>(scope);
        await MigrateAsync<UsersDbContext>(scope);
        await MigrateAsync<AiEngineDbContext>(scope);
        await MigrateAsync<AgentsDbContext>(scope);
    }

    private static async Task MigrateAsync<TDbContext>(AsyncServiceScope scope)
        where TDbContext : DbContext
    {
        TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        await context.Database.MigrateAsync();
    }
}
