using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class WarehouseZoneConfiguration : IEntityTypeConfiguration<WarehouseZone>
{
    public void Configure(EntityTypeBuilder<WarehouseZone> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.BuildingSection).HasMaxLength(100);
        builder.Property(e => e.TemperatureRange).HasMaxLength(50);
        builder.Property(e => e.HumidityRange).HasMaxLength(50);

        // Decimal precision
        builder.Property(e => e.Width).HasPrecision(18, 4);
        builder.Property(e => e.Length).HasPrecision(18, 4);
        builder.Property(e => e.Height).HasPrecision(18, 4);
        builder.Property(e => e.Area).HasPrecision(18, 4);

        builder.HasIndex(e => new { e.TenantId, e.WarehouseId })
            .HasDatabaseName("IX_WarehouseZones_Tenant_Warehouse");

        builder.HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique()
            .HasDatabaseName("IX_WarehouseZones_Tenant_Code");
    }
}
