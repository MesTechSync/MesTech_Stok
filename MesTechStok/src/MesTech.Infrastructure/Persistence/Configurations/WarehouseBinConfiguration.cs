using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class WarehouseBinConfiguration : IEntityTypeConfiguration<WarehouseBin>
{
    public void Configure(EntityTypeBuilder<WarehouseBin> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.BinType).HasMaxLength(50);

        builder.Property(e => e.Width).HasPrecision(18, 4);
        builder.Property(e => e.Depth).HasPrecision(18, 4);

        builder.HasIndex(e => new { e.TenantId, e.ShelfId })
            .HasDatabaseName("IX_WarehouseBins_Tenant_Shelf");

        builder.HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique()
            .HasDatabaseName("IX_WarehouseBins_Tenant_Code");
    }
}
