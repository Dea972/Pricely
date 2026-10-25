using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.AiEngine.Domain.LlmCalls;
using Pricely.Modules.AiEngine.Domain.Sessions;

namespace Pricely.Modules.AiEngine.Infrastructure.LlmCalls;

internal sealed class LlmCallConfiguration : IEntityTypeConfiguration<LlmCall>
{
    private const int CostPrecision = 18;
    private const int CostScale = 6;

    public void Configure(EntityTypeBuilder<LlmCall> builder)
    {
        builder.ToTable("llm_calls");

        builder.HasKey(c => c.Id);

        builder.Ignore(c => c.DomainEvents);
        builder.Ignore(c => c.TotalTokens);

        builder.Property(c => c.FeatureCode).HasMaxLength(100);
        builder.Property(c => c.ModelProvider).HasMaxLength(50);
        builder.Property(c => c.ModelName).HasMaxLength(150);
        builder.Property(c => c.ErrorMessage).HasMaxLength(LlmCall.ErrorMessageMaxLength);

        builder.Property(c => c.InputCost).HasPrecision(CostPrecision, CostScale);
        builder.Property(c => c.CachedInputCost).HasPrecision(CostPrecision, CostScale);
        builder.Property(c => c.CacheWriteCost).HasPrecision(CostPrecision, CostScale);
        builder.Property(c => c.OutputCost).HasPrecision(CostPrecision, CostScale);
        builder.Property(c => c.TotalCost).HasPrecision(CostPrecision, CostScale);

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(c => c.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.CalledOnUtc);
        builder.HasIndex(c => c.FeatureCode);
    }
}
