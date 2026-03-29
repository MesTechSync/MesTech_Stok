using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class StockLotConfiguration : IEntityTypeConfiguration<StockLot>
{
    public void Configure(EntityTypeBuilder<StockLot> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasIndex(l => new { l.TenantId, l.ProductId })
            .HasDatabaseName("IX_StockLots_Tenant_Product");

        builder.HasIndex(l => new { l.TenantId, l.LotNumber })
            .HasDatabaseName("IX_StockLots_Tenant_LotNumber")
            .IsUnique();

        builder.HasIndex(l => new { l.TenantId, l.ReceivedAt })
            .HasDatabaseName("IX_StockLots_Tenant_Received")
            .IsDescending(false, true);

        builder.Property(l => l.LotNumber).HasMaxLength(50).IsRequired();
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.SupplierName).HasMaxLength(200);
        builder.Property(l => l.WarehouseName).HasMaxLength(200);
        builder.Property(l => l.Notes).HasMaxLength(2000);

        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Warehouse).WithMany().HasForeignKey(l => l.WarehouseId).OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
