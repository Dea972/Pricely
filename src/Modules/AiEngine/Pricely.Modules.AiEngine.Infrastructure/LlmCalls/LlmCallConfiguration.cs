using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.AiEngine.Domain.LlmCalls;

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

        builder.Property(c => c.SessionId).HasMaxLength(LlmCall.SessionIdMaxLength);
        builder.Property(c => c.FeatureCode).HasMaxLength(LlmCall.FeatureCodeMaxLength);
        builder.Property(c => c.SourceModule).HasMaxLength(LlmCall.SourceModuleMaxLength);
        builder.Property(c => c.UserId).HasMaxLength(LlmCall.UserIdMaxLength);
        builder.Property(c => c.AgentName).HasMaxLength(LlmCall.AgentNameMaxLength);
        builder.Property(c => c.ModelProvider).HasMaxLength(LlmCall.ModelProviderMaxLength);
        builder.Property(c => c.ModelName).HasMaxLength(LlmCall.ModelNameMaxLength);
        builder.Property(c => c.ErrorMessage).HasMaxLength(LlmCall.ErrorMessageMaxLength);

        builder.Property(c => c.InputCost).HasPrecision(CostPrecision, CostScale);
        builder.Property(c => c.CachedInputCost).HasPrecision(CostPrecision, CostScale);
        builder.Property(c => c.CacheWriteCost).HasPrecision(CostPrecision, CostScale);
        builder.Property(c => c.OutputCost).HasPrecision(CostPrecision, CostScale);
        builder.Property(c => c.TotalCost).HasPrecision(CostPrecision, CostScale);

        builder.HasIndex(c => c.CalledOnUtc);
        builder.HasIndex(c => c.FeatureCode);
        builder.HasIndex(c => c.SessionId);
    }
}
