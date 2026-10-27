using Microsoft.EntityFrameworkCore;
using Pricely.Common.Infrastructure.Inbox;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.AgentActivities;
using Pricely.Modules.AiEngine.Domain.Features;
using Pricely.Modules.AiEngine.Domain.LlmCalls;
using Pricely.Modules.AiEngine.Domain.Models;
using Pricely.Modules.AiEngine.Domain.WebSearches;
using Pricely.Modules.AiEngine.Infrastructure.AgentActivities;
using Pricely.Modules.AiEngine.Infrastructure.Features;
using Pricely.Modules.AiEngine.Infrastructure.LlmCalls;
using Pricely.Modules.AiEngine.Infrastructure.Models;
using Pricely.Modules.AiEngine.Infrastructure.WebSearches;

namespace Pricely.Modules.AiEngine.Infrastructure.Database;

/// <summary>
/// AiEngine raises no domain events: no outbox, only the inbox that receives <c>LlmCalledIntegrationEvent</c>,
/// <c>AiToolInvokedIntegrationEvent</c> and <c>AiActionDecidedIntegrationEvent</c>.
/// </summary>
public sealed class AiEngineDbContext(DbContextOptions<AiEngineDbContext> options) : DbContext(options), IUnitOfWork
{
    internal DbSet<Model> Models { get; set; }

    internal DbSet<FeatureSetting> FeatureSettings { get; set; }

    internal DbSet<LlmCall> LlmCalls { get; set; }

    internal DbSet<AgentActivity> AgentActivities { get; set; }

    internal DbSet<WebSearch> WebSearches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.AiEngine);

        // Case-insensitive text columns (emails, names, codes): PostgreSQL text comparisons are case-sensitive (ADR-012).
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new ModelConfiguration());
        modelBuilder.ApplyConfiguration(new ModelPriceConfiguration());
        modelBuilder.ApplyConfiguration(new FeatureSettingConfiguration());
        modelBuilder.ApplyConfiguration(new LlmCallConfiguration());
        modelBuilder.ApplyConfiguration(new AgentActivityConfiguration());
        modelBuilder.ApplyConfiguration(new WebSearchConfiguration());
    }
}
