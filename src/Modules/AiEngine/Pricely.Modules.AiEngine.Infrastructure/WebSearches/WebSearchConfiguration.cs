using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.AiEngine.Domain.WebSearches;

namespace Pricely.Modules.AiEngine.Infrastructure.WebSearches;

internal sealed class WebSearchConfiguration : IEntityTypeConfiguration<WebSearch>
{
    public void Configure(EntityTypeBuilder<WebSearch> builder)
    {
        builder.ToTable("web_searches");

        builder.HasKey(s => s.Id);

        builder.Ignore(s => s.DomainEvents);

        builder.Property(s => s.SessionId).HasMaxLength(WebSearch.SessionIdMaxLength);
        builder.Property(s => s.Provider).HasMaxLength(WebSearch.ProviderMaxLength);
        builder.Property(s => s.UserId).HasMaxLength(WebSearch.UserIdMaxLength);
        builder.Property(s => s.ErrorMessage).HasMaxLength(WebSearch.ErrorMessageMaxLength);
        builder.Property(s => s.PricePerSearch).HasPrecision(18, 6);
        builder.Property(s => s.Cost).HasPrecision(18, 6);

        builder.HasIndex(s => s.SearchedOnUtc);
    }
}
