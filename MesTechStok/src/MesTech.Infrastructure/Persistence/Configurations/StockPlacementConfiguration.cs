using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class StockPlacementConfiguration : IEntityTypeConfiguration<StockPlacement>
{
    public void Configure(EntityTypeBuilder<StockPlacement> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.TenantId, p.WarehouseId, p.ShelfId, p.ProductId })
            .HasDatabaseName("IX_StockPlacements_Location_Product")
            .IsUnique();

        builder.HasIndex(p => new { p.TenantId, p.ProductId })
            .HasDatabaseName("IX_StockPlacements_Tenant_Product");

        builder.Property(p => p.WarehouseName).HasMaxLength(200);
        builder.Property(p => p.ShelfCode).HasMaxLength(50);
        builder.Property(p => p.BinCode).HasMaxLength(50);
        builder.Property(p => p.ProductName).HasMaxLength(300);
        builder.Property(p => p.ProductSku).HasMaxLength(100);

        builder.HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Warehouse).WithMany().HasForeignKey(p => p.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
