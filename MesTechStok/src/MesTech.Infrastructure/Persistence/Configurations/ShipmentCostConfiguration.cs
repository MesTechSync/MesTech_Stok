using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class ShipmentCostConfiguration : IEntityTypeConfiguration<ShipmentCost>
{
    public void Configure(EntityTypeBuilder<ShipmentCost> builder)
    {
        builder.HasIndex(s => new { s.TenantId, s.OrderId })
            .HasDatabaseName("IX_ShipmentCosts_Tenant_Order");

        builder.HasIndex(s => s.TrackingNumber)
            .HasFilter("\"TrackingNumber\" IS NOT NULL")
            .HasDatabaseName("IX_ShipmentCosts_Tracking");

        builder.Property(s => s.Cost).HasPrecision(18, 2);
        builder.Property(s => s.DesiWeight).HasPrecision(18, 4);
        builder.Property(s => s.CustomerChargeAmount).HasPrecision(18, 2);
        builder.Property(s => s.TrackingNumber).HasMaxLength(100);
        builder.Property(s => s.CargoBarcode).HasMaxLength(100);
    }
}
