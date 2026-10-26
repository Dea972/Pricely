using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Pricely.Modules.Agentic.Infrastructure.Database;
using Pricely.Modules.AiEngine.Infrastructure.Database;
using Pricely.Modules.Catalog.Infrastructure.Database;
using Pricely.Modules.Pricing.Infrastructure.Database;
using Pricely.Modules.Users.Infrastructure.Database;

namespace Pricely.ArchitectureTests;

/// <summary>
/// One schema per module and no foreign key between schemas (a plain Guid column, commented, instead).
/// </summary>
public sealed class PersistenceTests
{
    public static TheoryData<string, string> ModuleSchemas => new()
    {
        { nameof(CatalogDbContext), "catalog" },
        { nameof(PricingDbContext), "pricing" },
        { nameof(UsersDbContext), "users" },
        { nameof(AiEngineDbContext), "aiengine" },
        { nameof(AgenticDbContext), "agentic" }
    };

    [Theory]
    [MemberData(nameof(ModuleSchemas))]
    public void Tables_ShouldAllLiveInTheSchemaOfTheirModule(string contextName, string expectedSchema)
    {
        using DbContext context = CreateContext(contextName);

        foreach (IEntityType entityType in context.Model.GetEntityTypes().Where(e => e.GetTableName() is not null))
        {
            entityType.GetSchema().Should().Be(expectedSchema, $"{entityType.DisplayName()} belongs to {contextName}");
        }
    }

    [Theory]
    [MemberData(nameof(ModuleSchemas))]
    public void ForeignKeys_ShouldNeverCrossASchema(string contextName, string expectedSchema)
    {
        using DbContext context = CreateContext(contextName);

        foreach (IForeignKey foreignKey in context.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.PrincipalEntityType.GetSchema().Should().Be(expectedSchema);
            foreignKey.DeclaringEntityType.GetSchema().Should().Be(expectedSchema);
        }
    }

    private static DbContext CreateContext(string contextName) => contextName switch
    {
        nameof(CatalogDbContext) => new CatalogDbContext(Options<CatalogDbContext>()),
        nameof(PricingDbContext) => new PricingDbContext(Options<PricingDbContext>()),
        nameof(UsersDbContext) => new UsersDbContext(Options<UsersDbContext>()),
        nameof(AiEngineDbContext) => new AiEngineDbContext(Options<AiEngineDbContext>()),
        nameof(AgenticDbContext) => new AgenticDbContext(Options<AgenticDbContext>()),
        _ => throw new ArgumentOutOfRangeException(nameof(contextName))
    };

    private static DbContextOptions<TContext> Options<TContext>()
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>().UseNpgsql("Host=localhost").Options;
}
