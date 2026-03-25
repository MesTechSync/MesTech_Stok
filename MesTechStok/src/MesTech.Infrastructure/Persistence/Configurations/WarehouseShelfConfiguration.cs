using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class WarehouseShelfConfiguration : IEntityTypeConfiguration<WarehouseShelf>
{
    public void Configure(EntityTypeBuilder<WarehouseShelf> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.Accessibility).HasMaxLength(50);

        builder.HasIndex(e => new { e.TenantId, e.RackId })
            .HasDatabaseName("IX_WarehouseShelves_Tenant_Rack");

        builder.HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique()
            .HasDatabaseName("IX_WarehouseShelves_Tenant_Code");
    }
}
