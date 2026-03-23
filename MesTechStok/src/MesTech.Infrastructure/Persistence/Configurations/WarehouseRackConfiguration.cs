using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class WarehouseRackConfiguration : IEntityTypeConfiguration<WarehouseRack>
{
    public void Configure(EntityTypeBuilder<WarehouseRack> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.Orientation).HasMaxLength(50);
        builder.Property(e => e.RackType).HasMaxLength(50);

        builder.HasIndex(e => new { e.TenantId, e.ZoneId })
            .HasDatabaseName("IX_WarehouseRacks_Tenant_Zone");

        builder.HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique()
            .HasDatabaseName("IX_WarehouseRacks_Tenant_Code");
    }
}
