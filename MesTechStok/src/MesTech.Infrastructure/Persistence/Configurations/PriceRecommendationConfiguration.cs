using MesTech.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class PriceRecommendationConfiguration : IEntityTypeConfiguration<PriceRecommendation>
{
    public void Configure(EntityTypeBuilder<PriceRecommendation> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.RecommendedPrice).HasPrecision(18, 4);
        builder.Property(e => e.CurrentPrice).HasPrecision(18, 4);
        builder.Property(e => e.CompetitorMinPrice).HasPrecision(18, 4);
        builder.Property(e => e.Reasoning).HasMaxLength(2000);
        builder.Property(e => e.Source).HasMaxLength(100);
        builder.Property(e => e.Strategy).HasMaxLength(100);

        builder.HasIndex(e => new { e.TenantId, e.ProductId })
            .HasDatabaseName("IX_PriceRecommendations_Tenant_Product");
    }
}
