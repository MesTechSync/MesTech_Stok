using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasIndex(s => new { s.TenantId, s.ProductId, s.Date })
            .HasDatabaseName("IX_StockMovements_Tenant_Product_Date");

        builder.HasIndex(s => new { s.TenantId, s.Date })
            .HasDatabaseName("IX_StockMovements_Tenant_Date");

        builder.HasIndex(s => s.MovementType)
            .HasDatabaseName("IX_StockMovements_Type");

        builder.Property(s => s.Reason).HasMaxLength(500);
        builder.Property(s => s.ProcessedBy).HasMaxLength(100);
        builder.Property(s => s.UnitCost).HasPrecision(18, 2);
        builder.Property(s => s.TotalCost).HasPrecision(18, 2);
    }
}
