using MesTech.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class StockPredictionConfiguration : IEntityTypeConfiguration<StockPrediction>
{
    public void Configure(EntityTypeBuilder<StockPrediction> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Reasoning).HasMaxLength(2000);

        builder.HasIndex(e => new { e.TenantId, e.ProductId })
            .HasDatabaseName("IX_StockPredictions_Tenant_Product");
    }
}
