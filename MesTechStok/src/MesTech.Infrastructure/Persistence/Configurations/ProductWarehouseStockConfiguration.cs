using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ProductWarehouseStockConfiguration : IEntityTypeConfiguration<ProductWarehouseStock>
{
    public void Configure(EntityTypeBuilder<ProductWarehouseStock> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.FulfillmentCenter).HasMaxLength(100);

        builder.HasIndex(e => new { e.TenantId, e.ProductId, e.WarehouseId })
            .IsUnique()
            .HasDatabaseName("IX_ProductWarehouseStocks_Tenant_Product_Warehouse");

        builder.HasIndex(e => new { e.TenantId, e.WarehouseId })
            .HasDatabaseName("IX_ProductWarehouseStocks_Tenant_Warehouse");

        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property<uint>("xmin").HasColumnType("xid").IsConcurrencyToken();
    }
}
